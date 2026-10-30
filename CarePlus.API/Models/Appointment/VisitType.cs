using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CarePlus.API.Models.Appointment
{
    [Table("VisitTypes")]
    public class VisitType
    {
        [Key]
        [StringLength(40)]
        public string VisitTypeId { get; set; } = null!;

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = null!;

        [Required]
        [StringLength(250)]
        public string Description { get; set; } = null!;

        public bool IsActive { get; set; } = true;
    }
}
