using CarePlus.API.Models.MedicineBatch;
using Microsoft.EntityFrameworkCore;

namespace CarePlus.API.Data
{
    public static class MedicineBatchSeed
    {
        public static void Apply(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<MedicineBatch>().HasData(
                // Small-clinic example: approximately 3–6 retail packs per batch.
                Row("BAT-20261101090000-MED0001", "MED0001", 80, new DateTime(2027, 5, 31)),
                Row("BAT-20261101090100-MED0002", "MED0002", 120, new DateTime(2027, 10, 31)),
                Row("BAT-20261101090200-MED0003", "MED0003", 60, new DateTime(2027, 7, 31)),
                Row("BAT-20261101090300-MED0004", "MED0004", 96, new DateTime(2027, 6, 30)),
                Row("BAT-20261101090400-MED0005", "MED0005", 60, new DateTime(2027, 8, 31)),
                Row("BAT-20261101090500-MED0006", "MED0006", 80, new DateTime(2027, 9, 30)));
        }

        private static MedicineBatch Row(
            string batchId,
            string medicineId,
            int stockQuantity,
            DateTime expiryDate) => new()
        {
            BatchId = batchId,
            MedicineId = medicineId,
            StockQuantity = stockQuantity,
            ExpiryDate = expiryDate
        };
    }
}
