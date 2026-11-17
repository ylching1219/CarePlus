using CarePlus.API.Data;
using CarePlus.API.DTO.InvoiceDTO;
using CarePlus.API.Extension;
using CarePlus.API.Options;
using CarePlus.API.Services.CounterServices.Interface;
using CarePlus.API.Services.InvoiceServices.Interface;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Stripe;
using InvoiceRow = CarePlus.API.Models.Invoice.Invoice;
using PaymentRow = CarePlus.API.Models.Payment.Payment;

namespace CarePlus.API.Services.InvoiceServices
{
    public class InvoiceServices : IInvoiceServices
    {
        private readonly AppDbContext _db;
        private readonly ICounterServices _counters;
        private readonly StripeOptions _stripe;

        public InvoiceServices(AppDbContext db, ICounterServices counters, IOptions<StripeOptions> stripe)
        {
            _db = db;
            _counters = counters;
            _stripe = stripe.Value;
        }

        public async Task<ApiResponse<List<AdminBillingRecordResponse>>> ListForAdminAsync()
        {
            var today = DateTimeHelper.MalaysiaNow().Date;
            var invoices = await _db.Invoices
                .AsNoTracking()
                .Include(row => row.Patient)
                    .ThenInclude(patient => patient.User)
                .Where(row => row.PaymentStatus != InvoiceRow.AwaitingPickupStatus)
                .OrderByDescending(row => row.IssueAt)
                .ToListAsync();

            var invoiceIds = invoices.Select(row => row.InvoiceId).ToList();
            var payments = invoiceIds.Count == 0
                ? new List<PaymentRow>()
                : await _db.Payments
                    .AsNoTracking()
                    .Where(row => invoiceIds.Contains(row.InvoiceId))
                    .OrderByDescending(row => row.TransactionDate)
                    .ToListAsync();

            var paymentsByInvoice = payments
                .GroupBy(row => row.InvoiceId)
                .ToDictionary(group => group.Key, group => group.ToList());

            var result = invoices.Select(invoice =>
            {
                paymentsByInvoice.TryGetValue(invoice.InvoiceId, out var invoicePayments);
                invoicePayments ??= [];

                var paidPayment = invoicePayments.FirstOrDefault(row => IsPaidStatus(row.Status));
                var isPaid = IsPaidStatus(invoice.PaymentStatus) || paidPayment != null;
                var daysOverdue = !isPaid && invoice.DueDate.HasValue
                    ? Math.Max(0, (today - invoice.DueDate.Value.Date).Days)
                    : 0;
                var lateFee = daysOverdue * PaymentRow.LateFeePerDay;
                var paidAmount = paidPayment?.TotalAmount ?? (isPaid ? invoice.TotalAmount : 0m);
                var lastPayment = invoicePayments.FirstOrDefault();

                return new AdminBillingRecordResponse
                {
                    InvoiceId = invoice.InvoiceId,
                    PatientId = invoice.PatientId,
                    PatientName = invoice.Patient?.User?.Name ?? "Unknown patient",
                    PatientUsername = invoice.Patient?.User?.Username ?? "",
                    Source = invoice.OrderId != null ? "Medicine order"
                        : InvoiceVisitLabel.IsPrescription(invoice.VisitType) ? "Prescription"
                        : "Consultation",
                    RecordId = invoice.RecordRef ?? invoice.MedicalRecordId ?? invoice.OrderId ?? "",
                    DoctorName = invoice.DoctorName ?? "",
                    VisitType = InvoiceVisitLabel.Read(invoice.VisitType).Visit,
                    BilledForName = InvoiceVisitLabel.Read(invoice.VisitType).BilledFor,
                    IssuedAt = ToMalaysiaOffset(invoice.IssueAt),
                    DueAt = invoice.DueDate.HasValue ? ToMalaysiaOffset(invoice.DueDate.Value) : null,
                    Status = isPaid ? "Paid" : daysOverdue > 0 ? "Overdue" : "Pending",
                    Total = invoice.TotalAmount,
                    PaidAmount = paidAmount,
                    LateFee = lateFee,
                    AmountOutstanding = isPaid ? 0m : invoice.TotalAmount + lateFee,
                    DaysOverdue = daysOverdue,
                    PaymentMethod = lastPayment?.PaymentMethod,
                    PaidAt = paidPayment == null ? null : ToMalaysiaOffset(paidPayment.TransactionDate),
                };
            }).ToList();

            return ApiResponse<List<AdminBillingRecordResponse>>.Ok(result);
        }

        public async Task<ApiResponse<List<OutstandingInvoiceResponse>>> ListOutstandingAsync(long userId)
        {
            var patient = await _db.Patients.AsNoTracking().FirstOrDefaultAsync(row => row.UserId == userId);
            if (patient == null)
            {
                return ApiResponse<List<OutstandingInvoiceResponse>>.Fail("This account is not a patient account.", 400);
            }

            var today = DateTimeHelper.MalaysiaNow().Date;
            var rows = await _db.Invoices.AsNoTracking()
                .Where(row => row.PatientId == patient.PatientId && row.DueDate != null && row.PaymentStatus != "Paid")
                .OrderBy(row => row.DueDate)
                .ToListAsync();

            return ApiResponse<List<OutstandingInvoiceResponse>>.Ok(rows.Select(row => ToResponse(row, today)).ToList());
        }

        public async Task<ApiResponse<List<PaidInvoiceResponse>>> ListPaidAsync(long userId)
        {
            var patient = await _db.Patients.AsNoTracking().FirstOrDefaultAsync(row => row.UserId == userId);
            if (patient == null)
            {
                return ApiResponse<List<PaidInvoiceResponse>>.Fail("This account is not a patient account.", 400);
            }

            var rows = await _db.Payments.AsNoTracking()
                .Include(row => row.Invoice)
                    .ThenInclude(invoice => invoice.MedicalRecord)
                .Where(row => row.Invoice.PatientId == patient.PatientId
                    && row.Invoice.OrderId == null
                    && (row.Status == "Paid" || row.Status == "Completed"))
                .OrderByDescending(row => row.TransactionDate)
                .ToListAsync();

            var result = rows.Select(row =>
            {
                var invoice = row.Invoice;
                var overdueDays = row.DueDate == null
                    ? 0
                    : Math.Max(0, (row.TransactionDate.Date - row.DueDate.Value.Date).Days);
                var lateFee = Math.Max(0m, row.TotalAmount - invoice.TotalAmount);
                return new PaidInvoiceResponse
                {
                    Id = row.PaymentId,
                    RecordId = InvoiceVisitLabel.IsPrescription(invoice.VisitType) ? "" : invoice.RecordRef ?? invoice.MedicalRecordId ?? "",
                    IssuedAt = ToMalaysiaOffset(invoice.IssueAt),
                    PaidAt = ToMalaysiaOffset(row.TransactionDate),
                    Method = row.PaymentMethod,
                    DoctorId = invoice.MedicalRecord?.DoctorId ?? "",
                    DoctorName = invoice.DoctorName ?? "",
                    VisitType = InvoiceVisitLabel.Read(invoice.VisitType).Visit,
                    BilledForName = InvoiceVisitLabel.Read(invoice.VisitType).BilledFor,
                    Bill = invoice.TotalAmount,
                    LateFee = lateFee,
                    DaysOverdue = overdueDays,
                    Total = row.TotalAmount,
                };
            }).ToList();

            return ApiResponse<List<PaidInvoiceResponse>>.Ok(result);
        }

        public async Task<ApiResponse<OutstandingInvoiceResponse>> PayWithSavedCardAsync(long userId, string invoiceId, long cardId)
        {
            var card = await _db.PaymentCards.AsNoTracking().FirstOrDefaultAsync(row => row.Id == cardId && row.UserId == userId);
            if (card == null)
            {
                return ApiResponse<OutstandingInvoiceResponse>.Fail("Choose a saved card.", 400);
            }

            var parts = card.NumberHash.Split(':');
            if (parts.Length != 3 || parts[0] != "stripe")
            {
                return ApiResponse<OutstandingInvoiceResponse>.Fail("Add this card again from your profile so Stripe can store it.", 400);
            }

            var patient = await _db.Patients.AsNoTracking().FirstOrDefaultAsync(row => row.UserId == userId);
            if (patient != null && PatientAge.UnderTwelve(patient.DateOfBirth, DateTimeHelper.MalaysiaNow().Date))
            {
                return ApiResponse<OutstandingInvoiceResponse>.Fail("Children under 12 cannot pay clinic bills.", 403);
            }

            var invoice = patient == null
                ? null
                : await _db.Invoices.AsNoTracking().FirstOrDefaultAsync(row => row.InvoiceId == invoiceId && row.PatientId == patient.PatientId);
            if (invoice == null || invoice.DueDate == null || invoice.PaymentStatus.Equals("Paid", StringComparison.OrdinalIgnoreCase))
            {
                return ApiResponse<OutstandingInvoiceResponse>.Fail("That invoice is not open.", 400);
            }

            var cents = (long)Math.Round(ToResponse(invoice, DateTimeHelper.MalaysiaNow().Date).AmountDue * 100m, MidpointRounding.AwayFromZero);
            if (cents < 200)
            {
                return ApiResponse<OutstandingInvoiceResponse>.Fail("Amount is too small for Stripe.", 400);
            }
            if (string.IsNullOrWhiteSpace(_stripe.SecretKey))
            {
                return ApiResponse<OutstandingInvoiceResponse>.Fail("Stripe is not configured.", 503);
            }

            try
            {
                StripeConfiguration.ApiKey = _stripe.SecretKey;
                var intent = await new PaymentIntentService().CreateAsync(new PaymentIntentCreateOptions
                {
                    Amount = cents,
                    Currency = string.IsNullOrWhiteSpace(_stripe.Currency) ? "myr" : _stripe.Currency.Trim().ToLowerInvariant(),
                    Customer = parts[1],
                    PaymentMethod = parts[2],
                    Confirm = true,
                    OffSession = true,
                    Description = $"CarePlus invoice {invoice.InvoiceId}",
                    Metadata = new Dictionary<string, string>
                    {
                        ["invoiceId"] = invoice.InvoiceId,
                        ["userId"] = userId.ToString(),
                    },
                });
                if (!string.Equals(intent.Status, "succeeded", StringComparison.OrdinalIgnoreCase))
                {
                    return ApiResponse<OutstandingInvoiceResponse>.Fail("Stripe could not charge this card. Try another card or FPX.", 402);
                }
            }
            catch (StripeException)
            {
                return ApiResponse<OutstandingInvoiceResponse>.Fail("Stripe could not charge this card. Try another card or FPX.", 402);
            }

            return await PayAsync(userId, invoiceId, new InvoicePayRequest
            {
                PaymentMethod = $"{card.Brand} ···· {card.Last4}",
            });
        }

        public async Task<ApiResponse<OutstandingInvoiceResponse>> PayAsync(long userId, string invoiceId, InvoicePayRequest request)
        {
            var patient = await _db.Patients.FirstOrDefaultAsync(row => row.UserId == userId);
            if (patient == null)
            {
                return ApiResponse<OutstandingInvoiceResponse>.Fail("This account is not a patient account.", 400);
            }

            if (PatientAge.UnderTwelve(patient.DateOfBirth, DateTimeHelper.MalaysiaNow().Date))
            {
                return ApiResponse<OutstandingInvoiceResponse>.Fail("Children under 12 cannot pay clinic bills.", 403);
            }

            var method = request.PaymentMethod?.Trim() ?? "";
            if (method.Length == 0 || method.Length > 80)
            {
                return ApiResponse<OutstandingInvoiceResponse>.Fail("Choose a payment method.", 400);
            }

            var invoice = await _db.Invoices.FirstOrDefaultAsync(row => row.InvoiceId == invoiceId && row.PatientId == patient.PatientId);
            if (invoice == null || invoice.DueDate == null)
            {
                return ApiResponse<OutstandingInvoiceResponse>.Fail("That invoice is not open.", 400);
            }

            if (invoice.PaymentStatus.Equals("Paid", StringComparison.OrdinalIgnoreCase))
            {
                return ApiResponse<OutstandingInvoiceResponse>.Fail("That invoice is already paid.", 400);
            }

            var today = DateTimeHelper.MalaysiaNow().Date;
            var shown = ToResponse(invoice, today);
            var paymentId = await _counters.GenerateNextIdAsync("PaymentId");
            invoice.PaymentStatus = "Paid";
            _db.Payments.Add(new PaymentRow
            {
                PaymentId = paymentId,
                InvoiceId = invoice.InvoiceId,
                TotalAmount = shown.AmountDue,
                PaymentMethod = method,
                TransactionDate = DateTimeHelper.MalaysiaNow(),
                DueDate = invoice.DueDate,
                Status = "Paid",
            });
            await _db.SaveChangesAsync();
            return ApiResponse<OutstandingInvoiceResponse>.Ok(shown);
        }

        private static OutstandingInvoiceResponse ToResponse(Models.Invoice.Invoice invoice, DateTime today)
        {
            var due = invoice.DueDate!.Value.Date;
            var days = (today - due).Days;
            if (days < 0)
            {
                days = 0;
            }

            var lateFee = days * PaymentRow.LateFeePerDay;
            var issued = new DateTimeOffset(DateTime.SpecifyKind(invoice.IssueAt, DateTimeKind.Unspecified), TimeSpan.FromHours(8));
            var dueAt = new DateTimeOffset(due, TimeSpan.FromHours(8));
            return new OutstandingInvoiceResponse
            {
                Id = invoice.InvoiceId,
                RecordId = InvoiceVisitLabel.IsPrescription(invoice.VisitType) ? "" : invoice.RecordRef ?? "",
                IssuedAt = issued.ToString("yyyy-MM-ddTHH:mm:sszzz"),
                DueAt = dueAt.ToString("yyyy-MM-ddTHH:mm:sszzz"),
                Status = days > 0 ? "overdue" : "due",
                DoctorName = invoice.DoctorName ?? "",
                VisitType = InvoiceVisitLabel.Read(invoice.VisitType).Visit,
                BilledForName = InvoiceVisitLabel.Read(invoice.VisitType).BilledFor,
                Total = invoice.TotalAmount,
                DaysOverdue = days,
                LateFee = lateFee,
                AmountDue = invoice.TotalAmount + lateFee,
            };
        }

        private static string ToMalaysiaOffset(DateTime value)
        {
            return new DateTimeOffset(DateTime.SpecifyKind(value, DateTimeKind.Unspecified), TimeSpan.FromHours(8))
                .ToString("yyyy-MM-ddTHH:mm:sszzz");
        }

        private static bool IsPaidStatus(string? status)
        {
            return string.Equals(status, "Paid", StringComparison.OrdinalIgnoreCase)
                || string.Equals(status, "Completed", StringComparison.OrdinalIgnoreCase);
        }
    }
}
