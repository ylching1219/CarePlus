using System.ComponentModel.DataAnnotations;

namespace CarePlus.API.DTO.ClinicRecordDTO
{
    public class CreateDoctorReviewRequest
    {
        [Range(1, 5)]
        public int Rating { get; set; }

        [StringLength(1000)]
        public string? Description { get; set; }

        public bool IsAnonymous { get; set; }
    }

    public class DoctorReviewResponse
    {
        public int Rating { get; set; }
        public string? Description { get; set; }
        public bool IsAnonymous { get; set; }
        public string? AuthorName { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
