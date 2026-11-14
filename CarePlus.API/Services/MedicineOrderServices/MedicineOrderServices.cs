using CarePlus.API.Data;
using CarePlus.API.DTO.MedicineOrderDTO;
using CarePlus.API.Extension;
using CarePlus.API.Models.OrderItem;
using CarePlus.API.Options;
using InvoiceRow = CarePlus.API.Models.Invoice.Invoice;
using PaymentRow = CarePlus.API.Models.Payment.Payment;
using CarePlus.API.Services.CounterServices.Interface;
using CarePlus.API.Services.MedicineOrderServices.Interface;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using OrderRow = CarePlus.API.Models.MedicineOrder.MedicineOrder;
using Stripe;
using Stripe.Checkout;

namespace CarePlus.API.Services.MedicineOrderServices
{
    public class MedicineOrderServices : IMedicineOrderServices
    {
        private static readonly HashSet<string> StaffStatuses = new(StringComparer.OrdinalIgnoreCase)
        {
            "awaiting_payment",
            "paid",
            "preparing",
            "ready_pickup",
            "picked_up",
            "shipped",
            "delivered",
        };

        private readonly AppDbContext _db;
        private readonly AddressGeocoder _geocoder;
        private readonly ICounterServices _counters;
        private readonly StripeOptions _stripe;

        public MedicineOrderServices(
            AppDbContext db,
            AddressGeocoder geocoder,
            ICounterServices counters,
            IOptions<StripeOptions> stripe)
        {
            _db = db;
            _geocoder = geocoder;
            _counters = counters;
            _stripe = stripe.Value;
        }

        public async Task<ApiResponse<List<MedicineOrderResponse>>> ListAsync(long userId)
        {
            var patient = await _db.Patients.AsNoTracking().FirstOrDefaultAsync(row => row.UserId == userId);
            if (patient == null)
            {
                return ApiResponse<List<MedicineOrderResponse>>.Fail("This account is not a patient account.", 400);
            }

            await SettleDeliveredCashOrdersAsync(patient.PatientId);
            var orders = await _db.MedicineOrders.AsNoTracking()
                .Where(row => row.PatientId == patient.PatientId)
                .OrderByDescending(row => row.OrderDate)
                .ToListAsync();
            var ids = orders.Select(row => row.OrderId).ToList();
            var items = await _db.OrderItems.AsNoTracking()
                .Where(row => ids.Contains(row.OrderId))
                .ToListAsync();

            return ApiResponse<List<MedicineOrderResponse>>.Ok(orders.Select(row => ToResponse(row, items)).ToList());
        }

        public async Task<ApiResponse<List<MedicineOrderResponse>>> ListPaidAsync(long userId)
        {
            var patient = await _db.Patients.AsNoTracking().FirstOrDefaultAsync(row => row.UserId == userId);
            if (patient == null)
            {
                return ApiResponse<List<MedicineOrderResponse>>.Fail("This account is not a patient account.", 400);
            }

            await SettleDeliveredCashOrdersAsync(patient.PatientId);
            var paid = await _db.Payments.AsNoTracking()
                .Where(row => row.Invoice.PatientId == patient.PatientId && row.Status == "Paid" && row.Invoice.OrderId != null)
                .OrderByDescending(row => row.TransactionDate)
                .Select(row => new { row.Invoice.OrderId, row.PaymentMethod, row.TransactionDate, row.TotalAmount })
                .ToListAsync();
            var orderIds = paid.Select(row => row.OrderId!).Distinct().ToList();
            var orders = await _db.MedicineOrders.AsNoTracking()
                .Where(row => orderIds.Contains(row.OrderId))
                .ToListAsync();
            var items = await _db.OrderItems.AsNoTracking()
                .Where(row => orderIds.Contains(row.OrderId))
                .ToListAsync();

            var rows = paid.Select(row =>
            {
                var order = orders.First(item => item.OrderId == row.OrderId);
                var response = ToResponse(order, items);
                var paidAt = new DateTimeOffset(DateTime.SpecifyKind(row.TransactionDate, DateTimeKind.Unspecified), TimeSpan.FromHours(8));
                response.PlacedAt = paidAt.ToString("yyyy-MM-ddTHH:mm:sszzz");
                response.Payment = row.PaymentMethod;
                response.Total = row.TotalAmount;
                return response;
            }).ToList();

            return ApiResponse<List<MedicineOrderResponse>>.Ok(rows);
        }

        public async Task<ApiResponse<List<MedicineOrderResponse>>> ListForStaffAsync()
        {
            var orders = await _db.MedicineOrders
                .AsNoTracking()
                .Include(row => row.Patient)
                    .ThenInclude(patient => patient.User)
                .OrderByDescending(row => row.OrderDate)
                .ToListAsync();
            var ids = orders.Select(row => row.OrderId).ToList();
            var items = await _db.OrderItems.AsNoTracking()
                .Where(row => ids.Contains(row.OrderId))
                .ToListAsync();

            return ApiResponse<List<MedicineOrderResponse>>.Ok(
                orders.Select(row => ToResponse(row, items)).ToList());
        }

        public async Task<ApiResponse<MedicineOrderResponse>> UpdateStatusAsync(string orderId, string status)
        {
            var id = orderId?.Trim() ?? "";
            var normalized = CanonicalStatus(status ?? "");
            if (id.Length == 0 || !StaffStatuses.Contains(normalized))
            {
                return ApiResponse<MedicineOrderResponse>.Fail("Choose a valid medicine order status.", 400);
            }

            if (normalized == "paid")
            {
                return ApiResponse<MedicineOrderResponse>.Fail("Payment status is updated by the payment flow.", 400);
            }

            var order = await _db.MedicineOrders
                .Include(row => row.Patient)
                    .ThenInclude(patient => patient.User)
                .FirstOrDefaultAsync(row => row.OrderId == id);
            if (order == null)
            {
                return ApiResponse<MedicineOrderResponse>.Fail("Medicine order not found.", 404);
            }

            if ((normalized is "ready_pickup" or "picked_up") && !IsPickup(order.Fulfilment))
            {
                return ApiResponse<MedicineOrderResponse>.Fail("Pickup statuses are only valid for pickup orders.", 400);
            }

            if ((normalized is "shipped" or "delivered") && !IsDelivery(order.Fulfilment))
            {
                return ApiResponse<MedicineOrderResponse>.Fail("Delivery statuses are only valid for delivery orders.", 400);
            }

            var current = CanonicalStatus(order.Status);
            if (current is "completed" or "dispensed")
            {
                current = IsDelivery(order.Fulfilment) ? "delivered" : "picked_up";
            }

            if (!CanMoveToStatus(order.Fulfilment, current, normalized))
            {
                return ApiResponse<MedicineOrderResponse>.Fail("Order status cannot move backwards.", 409);
            }

            order.Status = normalized;
            await _db.SaveChangesAsync();
            await SettleDeliveredCashOrdersAsync(order.PatientId);

            var items = await _db.OrderItems.AsNoTracking()
                .Where(row => row.OrderId == order.OrderId)
                .ToListAsync();
            return ApiResponse<MedicineOrderResponse>.Ok(ToResponse(order, items));
        }

        public async Task<ApiResponse<MedicineOrderResponse>> PlaceAsync(long userId, MedicineOrderPlaceRequest request)
        {
            var patient = await _db.Patients.FirstOrDefaultAsync(row => row.UserId == userId);
            if (patient == null)
            {
                return ApiResponse<MedicineOrderResponse>.Fail("This account is not a patient account.", 400);
            }

            var fulfilment = request.Fulfilment?.Trim().ToLowerInvariant() ?? "";
            if (fulfilment != "pickup" && fulfilment != "delivery")
            {
                return ApiResponse<MedicineOrderResponse>.Fail("Choose pickup or delivery.", 400);
            }

            var lines = request.Lines ?? [];
            if (lines.Count == 0 || lines.Count > 20)
            {
                return ApiResponse<MedicineOrderResponse>.Fail("Add at least one medicine.", 400);
            }

            if (lines.Any(line => string.IsNullOrWhiteSpace(line.Name) || line.Qty is < 1 or > 5 || line.UnitPrice < 0))
            {
                return ApiResponse<MedicineOrderResponse>.Fail("Check the medicine name, quantity, and price.", 400);
            }

            var chosen = fulfilment == "delivery" ? await ChosenAddressAsync(patient, request.DeliveryAddressId) : null;
            if (fulfilment == "delivery" && chosen == null)
            {
                return ApiResponse<MedicineOrderResponse>.Fail("Add a delivery address before placing the order.", 400);
            }

            var fee = fulfilment == "delivery" ? request.DeliveryFee : 0;
            if (fee != 0 && fee != 7 && fee != 14)
            {
                return ApiResponse<MedicineOrderResponse>.Fail("Delivery fee is not valid.", 400);
            }

            var knownIds = lines
                .Select(line => line.MedicineId?.Trim())
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .Select(id => id!)
                .Distinct()
                .ToList();
            if (knownIds.Count != lines.Count)
            {
                return ApiResponse<MedicineOrderResponse>.Fail("That medicine is not available.", 400);
            }

            var medicines = await _db.Medicines
                .Where(row => knownIds.Contains(row.MedicineId) && row.Status == "Available")
                .ToListAsync();
            if (medicines.Count != knownIds.Count)
            {
                return ApiResponse<MedicineOrderResponse>.Fail("That medicine is not available.", 400);
            }

            var now = DateTimeHelper.MalaysiaNow();
            var bought = await MonthlyMedicineLimit.BoughtAsync(_db, patient.PatientId, now);
            if (lines.Any(line => bought.GetValueOrDefault(line.MedicineId!.Trim()) + line.Qty > MonthlyMedicineLimit.MaxUnits))
            {
                return ApiResponse<MedicineOrderResponse>.Fail("You can buy at most 5 units of the same medicine in one month.", 400);
            }

            var orderId = await _counters.GenerateNextIdAsync("OrderId");
            var savedLines = new List<OrderItem>();
            foreach (var line in lines)
            {
                var medicine = medicines.First(row => row.MedicineId == line.MedicineId!.Trim());
                var subtotal = decimal.Round(medicine.Price * line.Qty, 2, MidpointRounding.AwayFromZero);
                savedLines.Add(new OrderItem
                {
                    OrderItemId = await _counters.GenerateNextIdAsync("OrderItemId"),
                    OrderId = orderId,
                    MedicineId = medicine.MedicineId,
                    ItemName = medicine.MedicineName,
                    Detail = medicine.Pack,
                    UnitPrice = medicine.Price,
                    Quantity = line.Qty,
                    SubTotal = subtotal,
                });
            }

            var goods = savedLines.Sum(line => line.SubTotal);
            var order = new OrderRow
            {
                OrderId = orderId,
                PatientId = patient.PatientId,
                TotalAmount = goods + fee,
                DeliveryFee = fee,
                OrderDate = now,
                Status = "awaiting_payment",
                Fulfilment = fulfilment,
                PaymentMethod = (request.PaymentMethod ?? "").Trim(),
                DeliveryAddressId = chosen?.Id,
                DeliveryAddress = chosen?.Line,
                Latitude = chosen?.Latitude,
                Longitude = chosen?.Longitude,
            };

            if (order.PaymentMethod.Length > 80 || (chosen?.Line.Length ?? 0) > 300)
            {
                return ApiResponse<MedicineOrderResponse>.Fail("One of the order fields is too long.", 400);
            }

            var paid = false;
            if (!string.IsNullOrWhiteSpace(request.StripeSessionId))
            {
                var stripePaid = await VerifyStripeMedicinePaymentAsync(userId, request.StripeSessionId, order.TotalAmount, null);
                if (stripePaid != null)
                {
                    return stripePaid;
                }

                paid = true;
                order.Status = "paid";
                order.PaymentMethod = StripePaymentLabel(order.PaymentMethod, request.StripeSessionId);
            }

            var invoiceId = await _counters.GenerateNextIdAsync("InvoiceId");
            var paymentId = await _counters.GenerateNextIdAsync("PaymentId");
            _db.MedicineOrders.Add(order);
            _db.OrderItems.AddRange(savedLines);
            _db.Invoices.Add(new InvoiceRow
            {
                InvoiceId = invoiceId,
                PatientId = patient.PatientId,
                OrderId = orderId,
                TotalAmount = order.TotalAmount,
                IssueAt = now,
                PaymentStatus = paid ? "Paid" : "Pending",
            });
            _db.Payments.Add(new PaymentRow
            {
                PaymentId = paymentId,
                InvoiceId = invoiceId,
                TotalAmount = order.TotalAmount,
                PaymentMethod = order.PaymentMethod,
                TransactionDate = now,
                Status = paid ? "Paid" : "Pending",
            });

            await using var stockTx = await _db.Database.BeginTransactionAsync();
            if (!await TryReserveStockAsync(savedLines))
            {
                await stockTx.RollbackAsync();
                return ApiResponse<MedicineOrderResponse>.Fail("Not enough stock left for this order.", 409);
            }

            var cart = await _db.Carts.FirstOrDefaultAsync(row => row.PatientId == patient.PatientId);
            if (cart != null)
            {
                var cartItems = await _db.CartItems.Where(row => row.CartId == cart.CartId).ToListAsync();
                var boughtLines = cartItems.Where(row => knownIds.Contains(row.MedicineId)).ToList();
                _db.CartItems.RemoveRange(boughtLines);
                cart.TotalAmount = cartItems.Except(boughtLines).Sum(row => (decimal)row.Subtotal);
            }

            await _db.SaveChangesAsync();
            await stockTx.CommitAsync();
            return ApiResponse<MedicineOrderResponse>.Ok(ToResponse(order, savedLines));
        }

        private async Task<(long? Id, string Line, double? Latitude, double? Longitude)?> ChosenAddressAsync(Models.Patient.Patient patient, long? addressId)
        {
            if (addressId is long id)
            {
                var row = await _db.DeliveryAddresses.FirstOrDefaultAsync(address => address.Id == id && address.PatientId == patient.PatientId);
                if (row == null)
                {
                    return null;
                }

                var line = AddressGeocoder.StreetLine(row.Unit, row.Address);
                double? latitude = row.Latitude;
                double? longitude = row.Longitude;
                if (latitude == null || longitude == null)
                {
                    var point = await _geocoder.LocateAsync(line);
                    latitude = point?.Latitude;
                    longitude = point?.Longitude;
                    if (latitude != null && longitude != null)
                    {
                        row.Latitude = latitude;
                        row.Longitude = longitude;
                    }
                }

                return (row.Id, line, latitude, longitude);
            }

            var profileLine = patient.Address?.Trim();
            if (string.IsNullOrWhiteSpace(profileLine))
            {
                return null;
            }

            var profilePoint = await _geocoder.LocateAsync(profileLine);
            return (null, profileLine, profilePoint?.Latitude, profilePoint?.Longitude);
        }

        public async Task<ApiResponse<MapPointResponse?>> LocateAsync(string? address)
        {
            var point = await _geocoder.LocateAsync(address);
            if (point == null)
            {
                return ApiResponse<MapPointResponse?>.Ok(null, "No map match for this address.");
            }

            return ApiResponse<MapPointResponse?>.Ok(new MapPointResponse
            {
                Latitude = point.Value.Latitude,
                Longitude = point.Value.Longitude,
            });
        }

        public async Task<ApiResponse<MedicineOrderResponse>> PayAsync(long userId, string orderId, MedicineOrderPayRequest request)
        {
            var patient = await _db.Patients.FirstOrDefaultAsync(row => row.UserId == userId);
            if (patient == null)
            {
                return ApiResponse<MedicineOrderResponse>.Fail("This account is not a patient account.", 400);
            }

            var id = orderId?.Trim() ?? "";
            var order = await _db.MedicineOrders.FirstOrDefaultAsync(row => row.OrderId == id && row.PatientId == patient.PatientId);
            if (order == null)
            {
                return ApiResponse<MedicineOrderResponse>.Fail("Order not found.", 404);
            }

            var items = await _db.OrderItems.Where(row => row.OrderId == order.OrderId).ToListAsync();
            if (CanonicalStatus(order.Status) == "paid")
            {
                return ApiResponse<MedicineOrderResponse>.Ok(ToResponse(order, items));
            }

            if (CanonicalStatus(order.Status) != "awaiting_payment")
            {
                return ApiResponse<MedicineOrderResponse>.Fail("This order is not waiting for payment.", 400);
            }

            var stripePaid = await VerifyStripeMedicinePaymentAsync(userId, request?.StripeSessionId, order.TotalAmount, order.OrderId);
            if (stripePaid != null)
            {
                return stripePaid;
            }

            order.Status = "paid";
            order.PaymentMethod = StripePaymentLabel(order.PaymentMethod, request?.StripeSessionId);
            var invoice = await _db.Invoices.FirstOrDefaultAsync(row => row.OrderId == order.OrderId);
            if (invoice != null)
            {
                invoice.PaymentStatus = "Paid";
                var payment = await _db.Payments.FirstOrDefaultAsync(row => row.InvoiceId == invoice.InvoiceId);
                if (payment != null)
                {
                    payment.Status = "Paid";
                    payment.PaymentMethod = order.PaymentMethod;
                    payment.TransactionDate = DateTimeHelper.MalaysiaNow();
                }
            }

            await _db.SaveChangesAsync();
            return ApiResponse<MedicineOrderResponse>.Ok(ToResponse(order, items));
        }

        /// <summary>
        /// Hold the units when the order is saved. Rows are locked so two orders cannot take the last unit.
        /// </summary>
        private async Task<bool> TryReserveStockAsync(IReadOnlyList<OrderItem> lines)
        {
            var now = DateTimeHelper.MalaysiaNow();
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line.MedicineId) || line.Quantity < 1)
                {
                    return false;
                }

                var batches = await _db.MedicineBatches
                    .FromSqlRaw(
                        """
                        SELECT * FROM `MedicineBatches`
                        WHERE `MedicineId` = {0} AND `StockQuantity` > 0 AND `ExpiryDate` > {1}
                        ORDER BY `ExpiryDate`
                        FOR UPDATE
                        """,
                        line.MedicineId,
                        now)
                    .ToListAsync();

                var left = line.Quantity;
                foreach (var batch in batches)
                {
                    if (left == 0) break;
                    var take = Math.Min(batch.StockQuantity, left);
                    batch.StockQuantity -= take;
                    left -= take;
                }

                if (left > 0) return false;
            }

            return true;
        }

        private static bool IsCashOnDelivery(string method)
        {
            var value = method.Trim();
            return value.Equals("COD", StringComparison.OrdinalIgnoreCase)
                || value.Equals("Cash on delivery", StringComparison.OrdinalIgnoreCase)
                || value.Contains("货到付款", StringComparison.Ordinal);
        }

        /// <returns>Failure response, or null when the Stripe session is paid for this user.</returns>
        private static string StripePaymentLabel(string method, string? sessionId)
        {
            var shortId = sessionId?.Trim() ?? "";
            if (shortId.Length == 0) return method;
            shortId = shortId.Length > 8 ? shortId[^8..] : shortId;
            return Truncate($"{method} · Stripe {shortId}", 80);
        }

        private async Task<ApiResponse<MedicineOrderResponse>?> VerifyStripeMedicinePaymentAsync(
            long userId,
            string? sessionId,
            decimal expectedTotal,
            string? expectedOrderId)
        {
            var id = sessionId?.Trim() ?? "";
            if (id.Length == 0)
            {
                return ApiResponse<MedicineOrderResponse>.Fail(
                    "Card and FPX orders need a completed Stripe checkout session.",
                    400);
            }

            if (string.IsNullOrWhiteSpace(_stripe.SecretKey))
            {
                return ApiResponse<MedicineOrderResponse>.Fail("Stripe is not configured.", 503);
            }

            Session session;
            try
            {
                StripeConfiguration.ApiKey = _stripe.SecretKey;
                session = await new SessionService().GetAsync(id);
            }
            catch (StripeException)
            {
                return ApiResponse<MedicineOrderResponse>.Fail("Stripe session could not be verified.", 400);
            }

            if (!string.Equals(session.ClientReferenceId, userId.ToString(), StringComparison.Ordinal)
                && (!session.Metadata.TryGetValue("userId", out var metaUser) || metaUser != userId.ToString()))
            {
                return ApiResponse<MedicineOrderResponse>.Fail("This checkout belongs to another account.", 403);
            }

            if (session.Metadata.TryGetValue("kind", out var kind)
                && !string.Equals(kind, "medicine", StringComparison.OrdinalIgnoreCase))
            {
                return ApiResponse<MedicineOrderResponse>.Fail("This Stripe session is not a medicine checkout.", 400);
            }

            if (!string.IsNullOrWhiteSpace(expectedOrderId)
                && session.Metadata.TryGetValue("orderId", out var metaOrder)
                && !string.Equals(metaOrder, expectedOrderId, StringComparison.Ordinal))
            {
                return ApiResponse<MedicineOrderResponse>.Fail("This payment is for a different order.", 400);
            }

            if (!string.Equals(session.PaymentStatus, "paid", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(session.Status, "complete", StringComparison.OrdinalIgnoreCase))
            {
                return ApiResponse<MedicineOrderResponse>.Fail("Payment is not complete yet.", 400);
            }

            if (session.Metadata.TryGetValue("amountCents", out var amountRaw)
                && long.TryParse(amountRaw, out var amountCents))
            {
                var paidAmount = amountCents / 100m;
                if (Math.Abs(paidAmount - expectedTotal) > 0.009m)
                {
                    return ApiResponse<MedicineOrderResponse>.Fail("Paid amount does not match the order total.", 400);
                }
            }

            return null;
        }

        private static string Truncate(string value, int max)
            => value.Length <= max ? value : value[..max];

        private async Task SettleDeliveredCashOrdersAsync(string patientId)
        {
            var pending = await _db.Payments
                .Include(row => row.Invoice)
                .ThenInclude(row => row.MedicineOrder)
                .Where(row => row.Status != "Paid" && row.Invoice.PatientId == patientId && row.Invoice.OrderId != null)
                .ToListAsync();
            var changed = false;
            foreach (var payment in pending)
            {
                var order = payment.Invoice.MedicineOrder;
                if (order == null || !CashCollected(order))
                {
                    continue;
                }

                if (!IsCashOnDelivery(payment.PaymentMethod) && !IsCashOnDelivery(order.PaymentMethod))
                {
                    continue;
                }

                payment.Status = "Paid";
                payment.TransactionDate = DateTimeHelper.MalaysiaNow();
                payment.Invoice.PaymentStatus = "Paid";
                changed = true;
            }

            if (changed)
            {
                await _db.SaveChangesAsync();
            }
        }

        private static bool CashCollected(OrderRow order)
        {
            var status = CanonicalStatus(order.Status);
            if (order.Fulfilment.Equals("pickup", StringComparison.OrdinalIgnoreCase))
            {
                return status is "picked_up" or "completed" or "dispensed";
            }

            return status is "delivered" or "completed" or "dispensed";
        }

        private static string ShownStatus(OrderRow order)
        {
            // ponytail: admin/DB may store "Picked_up" / "Picked Up"; always emit snake_case
            var status = CanonicalStatus(order.Status);
            if (status is "completed" or "dispensed")
            {
                return order.Fulfilment.Equals("delivery", StringComparison.OrdinalIgnoreCase)
                    ? "delivered"
                    : "picked_up";
            }
            return status;
        }

        private static string CanonicalStatus(string status) =>
            System.Text.RegularExpressions.Regex.Replace(status.Trim().ToLowerInvariant(), @"[\s-]+", "_");

        private static bool IsPickup(string fulfilment) =>
            fulfilment.Equals("pickup", StringComparison.OrdinalIgnoreCase);

        private static bool IsDelivery(string fulfilment) =>
            fulfilment.Equals("delivery", StringComparison.OrdinalIgnoreCase);

        private static bool CanMoveToStatus(string fulfilment, string current, string next)
        {
            if (current == next) return true;

            var rank = IsDelivery(fulfilment)
                ? new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
                {
                    ["awaiting_payment"] = 0,
                    ["paid"] = 0,
                    ["preparing"] = 1,
                    ["shipped"] = 2,
                    ["delivered"] = 3,
                }
                : new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
                {
                    ["awaiting_payment"] = 0,
                    ["paid"] = 0,
                    ["preparing"] = 1,
                    ["ready_pickup"] = 2,
                    ["picked_up"] = 3,
                };

            return rank.TryGetValue(current, out var currentRank)
                && rank.TryGetValue(next, out var nextRank)
                && nextRank >= currentRank;
        }

        private static MedicineOrderResponse ToResponse(OrderRow order, IEnumerable<OrderItem> items)
        {
            var placed = new DateTimeOffset(DateTime.SpecifyKind(order.OrderDate, DateTimeKind.Unspecified), TimeSpan.FromHours(8));
            return new MedicineOrderResponse
            {
                Id = order.OrderId,
                PatientId = order.PatientId,
                PatientName = order.Patient?.User?.Name ?? order.PatientId,
                PlacedAt = placed.ToString("yyyy-MM-ddTHH:mm:sszzz"),
                Status = ShownStatus(order),
                Fulfilment = order.Fulfilment,
                Payment = order.PaymentMethod,
                Address = order.DeliveryAddress,
                DeliveryFee = order.DeliveryFee,
                Total = order.TotalAmount,
                Latitude = order.Latitude,
                Longitude = order.Longitude,
                Lines = items.Where(item => item.OrderId == order.OrderId).Select(item => new MedicineOrderLineResponse
                {
                    Id = item.MedicineId ?? item.OrderItemId,
                    Name = item.ItemName,
                    Detail = item.Detail ?? "",
                    UnitPrice = item.UnitPrice,
                    Qty = item.Quantity,
                }).ToList(),
            };
        }
    }
}
