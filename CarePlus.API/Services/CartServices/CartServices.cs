using CarePlus.API.Data;
using CarePlus.API.DTO.CartDTO;
using CarePlus.API.Extension;
using CarePlus.API.Models.CartItem;
using CarePlus.API.Services.CartServices.Interface;
using CarePlus.API.Services.CounterServices.Interface;
using Microsoft.EntityFrameworkCore;
using CartRow = CarePlus.API.Models.Cart.Cart;

namespace CarePlus.API.Services.CartServices
{
    public class CartServices : ICartServices
    {
        private readonly AppDbContext _db;
        private readonly ICounterServices _counters;

        public CartServices(AppDbContext db, ICounterServices counters)
        {
            _db = db;
            _counters = counters;
        }

        public async Task<ApiResponse<CartStateResponse>> ListAsync(long userId)
        {
            var patient = await _db.Patients.AsNoTracking().FirstOrDefaultAsync(row => row.UserId == userId);
            if (patient == null)
            {
                return ApiResponse<CartStateResponse>.Fail("This account is not a patient account.", 400);
            }

            return ApiResponse<CartStateResponse>.Ok(await StateAsync(patient.PatientId));
        }

        public async Task<ApiResponse<CartStateResponse>> SetQtyAsync(long userId, CartQtyRequest request)
        {
            var patient = await _db.Patients.FirstOrDefaultAsync(row => row.UserId == userId);
            if (patient == null)
            {
                return ApiResponse<CartStateResponse>.Fail("This account is not a patient account.", 400);
            }

            var medicineId = request.MedicineId?.Trim() ?? "";
            var qty = request.Qty;
            if (medicineId.Length == 0 || qty < 0)
            {
                return ApiResponse<CartStateResponse>.Fail("Check the medicine and quantity.", 400);
            }

            if (qty > 0)
            {
                var bought = await MonthlyMedicineLimit.BoughtAsync(_db, patient.PatientId, DateTimeHelper.MalaysiaNow());
                var already = bought.GetValueOrDefault(medicineId);
                if (already + qty > MonthlyMedicineLimit.MaxUnits)
                {
                    return ApiResponse<CartStateResponse>.Fail("You can buy at most 5 units of the same medicine in one month.", 400);
                }
            }

            var medicine = await _db.Medicines.AsNoTracking().FirstOrDefaultAsync(row => row.MedicineId == medicineId);
            if (medicine == null || medicine.Status != "Available")
            {
                return ApiResponse<CartStateResponse>.Fail("That medicine is not available.", 400);
            }

            var cart = await EnsureCartAsync(patient.PatientId);
            var item = await _db.CartItems.FirstOrDefaultAsync(row => row.CartId == cart.CartId && row.MedicineId == medicineId);
            if (qty == 0)
            {
                if (item != null)
                {
                    _db.CartItems.Remove(item);
                }
            }
            else if (item == null)
            {
                _db.CartItems.Add(new CartItem
                {
                    CartItemId = await _counters.GenerateNextIdAsync("CartItemId"),
                    CartId = cart.CartId,
                    MedicineId = medicineId,
                    Quantity = qty,
                    Subtotal = (double)decimal.Round(medicine.Price * qty, 2, MidpointRounding.AwayFromZero)
                });
            }
            else
            {
                item.Quantity = qty;
                item.Subtotal = (double)decimal.Round(medicine.Price * qty, 2, MidpointRounding.AwayFromZero);
            }

            await SaveTotalAsync(cart);
            return ApiResponse<CartStateResponse>.Ok(await StateAsync(patient.PatientId));
        }

        public async Task<ApiResponse<CartStateResponse>> RemoveAsync(long userId, IReadOnlyCollection<string> medicineIds)
        {
            var patient = await _db.Patients.FirstOrDefaultAsync(row => row.UserId == userId);
            if (patient == null)
            {
                return ApiResponse<CartStateResponse>.Fail("This account is not a patient account.", 400);
            }

            var cart = await _db.Carts.FirstOrDefaultAsync(row => row.PatientId == patient.PatientId);
            if (cart != null)
            {
                var drop = medicineIds.Where(id => !string.IsNullOrWhiteSpace(id)).Select(id => id.Trim()).ToHashSet();
                var items = await _db.CartItems.Where(row => row.CartId == cart.CartId && drop.Contains(row.MedicineId)).ToListAsync();
                _db.CartItems.RemoveRange(items);
                await SaveTotalAsync(cart);
            }

            return ApiResponse<CartStateResponse>.Ok(await StateAsync(patient.PatientId));
        }

        public async Task<ApiResponse<CartStateResponse>> ClearAsync(long userId)
        {
            var patient = await _db.Patients.FirstOrDefaultAsync(row => row.UserId == userId);
            if (patient == null)
            {
                return ApiResponse<CartStateResponse>.Fail("This account is not a patient account.", 400);
            }

            var cart = await _db.Carts.FirstOrDefaultAsync(row => row.PatientId == patient.PatientId);
            if (cart != null)
            {
                var items = await _db.CartItems.Where(row => row.CartId == cart.CartId).ToListAsync();
                _db.CartItems.RemoveRange(items);
                cart.TotalAmount = 0;
                await _db.SaveChangesAsync();
            }

            return ApiResponse<CartStateResponse>.Ok(await StateAsync(patient.PatientId));
        }

        private async Task<CartRow> EnsureCartAsync(string patientId)
        {
            var cart = await _db.Carts.FirstOrDefaultAsync(row => row.PatientId == patientId);
            if (cart != null)
            {
                return cart;
            }

            cart = new CartRow
            {
                CartId = await _counters.GenerateNextIdAsync("CartId"),
                PatientId = patientId,
                TotalAmount = 0
            };
            _db.Carts.Add(cart);
            return cart;
        }

        private async Task SaveTotalAsync(CartRow cart)
        {
            await _db.SaveChangesAsync();
            var subtotals = await _db.CartItems.Where(row => row.CartId == cart.CartId).Select(row => row.Subtotal).ToListAsync();
            cart.TotalAmount = subtotals.Aggregate(0m, (sum, value) => sum + (decimal)value);
            await _db.SaveChangesAsync();
        }

        private async Task<List<CartLineResponse>> LinesAsync(string patientId)
        {
            var cart = await _db.Carts.AsNoTracking().FirstOrDefaultAsync(row => row.PatientId == patientId);
            if (cart == null)
            {
                return [];
            }

            var items = await _db.CartItems.AsNoTracking().Where(row => row.CartId == cart.CartId).ToListAsync();
            var ids = items.Select(row => row.MedicineId).ToList();
            var medicines = await _db.Medicines.AsNoTracking().Where(row => ids.Contains(row.MedicineId)).ToListAsync();
            return items
                .Select(item =>
                {
                    var medicine = medicines.FirstOrDefault(row => row.MedicineId == item.MedicineId);
                    return medicine == null
                        ? null
                        : new CartLineResponse
                        {
                            MedicineId = item.MedicineId,
                            Name = medicine.MedicineName,
                            UnitPrice = medicine.Price,
                            Qty = item.Quantity
                        };
                })
                .Where(line => line != null)
                .Cast<CartLineResponse>()
                .ToList();
        }

        private async Task<CartStateResponse> StateAsync(string patientId)
        {
            var bought = await MonthlyMedicineLimit.BoughtAsync(_db, patientId, DateTimeHelper.MalaysiaNow());
            return new CartStateResponse
            {
                Lines = await LinesAsync(patientId),
                Bought = bought.Select(pair => new MonthlyBoughtResponse { MedicineId = pair.Key, Qty = pair.Value }).ToList(),
            };
        }
    }
}
