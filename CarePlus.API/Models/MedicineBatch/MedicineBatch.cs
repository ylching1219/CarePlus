using CarePlus.API.Models.Medicine;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CarePlus.API.Models.MedicineBatch
{
    [Table("MedicineBatches")]
    public class MedicineBatch
    {
        [Key]
        public string BatchId { get; set; } = null!;

        [Required]
        public string MedicineId { get; set; } = null!;

        public Medicine.Medicine Medicine { get; set; } = null!;

        public int StockQuantity { get; set; }

        public DateTime ExpiryDate { get; set; }
    }
    
}
