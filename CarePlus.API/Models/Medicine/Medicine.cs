using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CarePlus.API.Models.Medicine
{
    [Table("Medicines")]
    public class Medicine
    {
        [Key]
        public string MedicineId { get; set; } = null!;

        [Required]
        [StringLength(100)]
        public string MedicineName { get; set; } = null!;

        [Required]
        [StringLength(200)]
        public string Description { get; set; } = null!;

        [Required]

        public decimal Price { get; set; }

        public string Status { get; set; } = "Available";
    }
}
