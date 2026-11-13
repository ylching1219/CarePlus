using CarePlus.API.Data;
using CarePlus.API.DTO.MedicineOrderDTO;
using CarePlus.API.Extension;
using CarePlus.API.Models.OrderItem;
using InvoiceRow = CarePlus.API.Models.Invoice.Invoice;
using PaymentRow = CarePlus.API.Models.Payment.Payment;
using CarePlus.API.Services.CounterServices.Interface;
using CarePlus.API.Services.MedicineOrderServices.Interface;
using Microsoft.EntityFrameworkCore;
using OrderRow = CarePlus.API.Models.MedicineOrder.MedicineOrder;

namespace CarePlus.API.Services.MedicineOrderServices
{
    public class MedicineOrderServices : IMedicineOrderServices
    {
        private readonly AppDbContext _db;
        private readonly AddressGeocoder _geocoder;
        private readonly ICounterServices _counters;

        public MedicineOrderServices(AppDbContext db, AddressGeocoder geocoder, ICounterServices counters)
        {
            _db = db;
            _geocoder = geocoder;
            _counters = counters;
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
                Status = fulfilment == "pickup" ? "ready_pickup" : "processing",
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

            var cashOnDelivery = IsCashOnDelivery(order.PaymentMethod);
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
                PaymentStatus = cashOnDelivery ? "Pending" : "Paid",
            });
            _db.Payments.Add(new PaymentRow
            {
                PaymentId = paymentId,
                InvoiceId = invoiceId,
                TotalAmount = order.TotalAmount,
                PaymentMethod = order.PaymentMethod,
                TransactionDate = now,
                Status = cashOnDelivery ? "Pending" : "Paid",
            });

            var cart = await _db.Carts.FirstOrDefaultAsync(row => row.PatientId == patient.PatientId);
            if (cart != null)
            {
                var cartItems = await _db.CartItems.Where(row => row.CartId == cart.CartId).ToListAsync();
                var boughtLines = cartItems.Where(row => knownIds.Contains(row.MedicineId)).ToList();
                _db.CartItems.RemoveRange(boughtLines);
                cart.TotalAmount = cartItems.Except(boughtLines).Sum(row => (decimal)row.Subtotal);
            }

            await _db.SaveChangesAsync();
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

        private static bool IsCashOnDelivery(string method)
        {
            var value = method.Trim();
            return value.Equals("COD", StringComparison.OrdinalIgnoreCase)
                || value.Equals("Cash on delivery", StringComparison.OrdinalIgnoreCase)
                || value.Contains("货到付款", StringComparison.Ordinal);
        }

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
            var status = order.Status.Trim();
            if (order.Fulfilment.Equals("pickup", StringComparison.OrdinalIgnoreCase))
            {
                return status.Equals("picked_up", StringComparison.OrdinalIgnoreCase);
            }

            return status.Equals("delivered", StringComparison.OrdinalIgnoreCase);
        }

        private static string ShownStatus(OrderRow order)
        {
            var status = order.Status.Trim().ToLowerInvariant();
            if (order.Fulfilment.Equals("pickup", StringComparison.OrdinalIgnoreCase)
                && (status == "processing" || status == "pending"))
            {
                return "ready_pickup";
            }

            return status;
        }

        private static MedicineOrderResponse ToResponse(OrderRow order, IEnumerable<OrderItem> items)
        {
            var placed = new DateTimeOffset(DateTime.SpecifyKind(order.OrderDate, DateTimeKind.Unspecified), TimeSpan.FromHours(8));
            return new MedicineOrderResponse
            {
                Id = order.OrderId,
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
