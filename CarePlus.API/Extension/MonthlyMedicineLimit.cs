using CarePlus.API.Data;
using Microsoft.EntityFrameworkCore;

namespace CarePlus.API.Extension
{
    public static class MonthlyMedicineLimit
    {
        public const int MaxUnits = 5;

        public static async Task<Dictionary<string, int>> BoughtAsync(AppDbContext db, string patientId, DateTime now)
        {
            var start = new DateTime(now.Year, now.Month, 1);
            var end = start.AddMonths(1);
            var orderIds = await db.MedicineOrders.AsNoTracking()
                .Where(row => row.PatientId == patientId && row.OrderDate >= start && row.OrderDate < end)
                .Select(row => row.OrderId)
                .ToListAsync();
            if (orderIds.Count == 0)
            {
                return [];
            }

            var items = await db.OrderItems.AsNoTracking()
                .Where(row => orderIds.Contains(row.OrderId) && row.MedicineId != null)
                .Select(row => new { row.MedicineId, row.Quantity })
                .ToListAsync();
            return items
                .GroupBy(row => row.MedicineId!)
                .ToDictionary(group => group.Key, group => group.Sum(row => row.Quantity));
        }
    }
}
