using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CarePlus.API.Models.DeliveryAddress
{
    [Table("DeliveryAddresses")]
    public class DeliveryAddress
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        [Required]
        public string PatientId { get; set; } = null!;

        public Patient.Patient Patient { get; set; } = null!;

        [Required]
        [MaxLength(20)]
        public string Label { get; set; } = null!;

        [Required]
        [MaxLength(30)]
        public string Unit { get; set; } = null!;

        [Required]
        [MaxLength(200)]
        public string Address { get; set; } = null!;

        public bool IsDefault { get; set; }

        public double? Latitude { get; set; }

        public double? Longitude { get; set; }
    }
}
