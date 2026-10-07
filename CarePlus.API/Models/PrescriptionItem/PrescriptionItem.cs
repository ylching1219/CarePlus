using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CarePlus.API.Models.PrescriptionItem
{
    [Table("PrescriptionItems")]
    public class PrescriptionItem
    {
        [Key]
        public string PrescriptionItemId { get; set; } = null!;

        [Required]
        public string PrescriptionId { get; set; } = null!;
        public Prescription.Prescription Prescription { get; set; } = null!;

        [Required]
        public string MedicineId { get; set; } = null!;
        public Medicine.Medicine Medicine { get; set; } = null!;

        [Required]
        public string Dosage { get; set; } = null!;

        [Required]
        public int Quantity { get; set; }

        [Required]
        public string Instructions { get; set; } = null!; 
    }
}
