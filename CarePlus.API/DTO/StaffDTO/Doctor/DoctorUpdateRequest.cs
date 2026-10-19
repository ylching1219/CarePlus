using System.ComponentModel.DataAnnotations;

namespace CarePlus.API.DTO.StaffDTO
{
    public record DoctorUpdateRequest
    {
        [Required(ErrorMessage = "Staff Id is required.")]
        public string StaffId { get; init; } = string.Empty;

        [StringLength(50)]
        public string? LicenseNumber { get; init; }

        [StringLength(100)]
        public string? Specialization { get; init; }

        [StringLength(150)]
        public string? Qualification { get; init; }

        [Range(0, 80, ErrorMessage = "Years of experience must be between 0 and 80.")]
        public int? YearsOfExperience { get; init; }
    }
}
