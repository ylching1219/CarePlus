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

        [StringLength(100)]
        public string? NameZh { get; set; }

        [Required]
        [StringLength(500)]
        public string Description { get; set; } = null!;

        [StringLength(500)]
        public string? DescriptionZh { get; set; }

        [Required]
        [StringLength(40)]
        public string Pack { get; set; } = "";

        [StringLength(40)]
        public string? PackZh { get; set; }

        [Required]
        [StringLength(20)]
        public string Form { get; set; } = "tablet";

        [Required]
        [StringLength(30)]
        public string Kind { get; set; } = "analgesic";

        [Required]
        [StringLength(160)]
        public string Method { get; set; } = "";

        [StringLength(160)]
        public string? MethodZh { get; set; }

        [Required]
        [StringLength(10)]
        public string ImageKey { get; set; } = "para";

        [Required]
        public decimal Price { get; set; }

        public string Status { get; set; } = "Available";
    }
}
