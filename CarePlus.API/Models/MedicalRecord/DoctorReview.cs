using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CarePlus.API.Models.MedicalRecord
{
    [Table("DoctorReviews")]
    public class DoctorReview
    {
        [Key]
        public string MedicalRecordId { get; set; } = null!;

        public MedRecord MedicalRecord { get; set; } = null!;

        [Range(1, 5)]
        public int Rating { get; set; }

        [MaxLength(1000)]
        public string? Description { get; set; }

        public bool IsAnonymous { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}
