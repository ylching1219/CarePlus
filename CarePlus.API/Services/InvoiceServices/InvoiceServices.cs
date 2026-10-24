using CarePlus.API.Data;
using CarePlus.API.DTO.InvoiceDTO;
using CarePlus.API.Extension;
using CarePlus.API.Services.CounterServices.Interface;
using CarePlus.API.Services.InvoiceServices.Interface;
using Microsoft.EntityFrameworkCore;
using PaymentRow = CarePlus.API.Models.Payment.Payment;

namespace CarePlus.API.Services.InvoiceServices
{
    public class InvoiceServices : IInvoiceServices
    {
        private readonly AppDbContext _db;
        private readonly ICounterServices _counters;

        public InvoiceServices(AppDbContext db, ICounterServices counters)
        {
            _db = db;
            _counters = counters;
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

        public async Task<ApiResponse<OutstandingInvoiceResponse>> PayAsync(long userId, string invoiceId, InvoicePayRequest request)
        {
            var patient = await _db.Patients.FirstOrDefaultAsync(row => row.UserId == userId);
            if (patient == null)
            {
                return ApiResponse<OutstandingInvoiceResponse>.Fail("This account is not a patient account.", 400);
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
                RecordId = invoice.RecordRef ?? "",
                IssuedAt = issued.ToString("yyyy-MM-ddTHH:mm:sszzz"),
                DueAt = dueAt.ToString("yyyy-MM-ddTHH:mm:sszzz"),
                Status = days > 0 ? "overdue" : "due",
                DoctorName = invoice.DoctorName ?? "",
                VisitType = invoice.VisitType ?? "",
                Total = invoice.TotalAmount,
                DaysOverdue = days,
                LateFee = lateFee,
                AmountDue = invoice.TotalAmount + lateFee,
            };
        }
    }
}
