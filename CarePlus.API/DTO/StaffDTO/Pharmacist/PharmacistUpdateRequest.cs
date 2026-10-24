using System.ComponentModel.DataAnnotations;

namespace CarePlus.API.DTO.StaffDTO
{
    public record PharmacistUpdateRequest : StaffUserUpdateRequest
    {
        [StringLength(50)]
        public string? LicenseNumber { get; init; }

        [StringLength(150)]
        public string? Qualification { get; init; }

        [StringLength(150)]
        public string? Certification { get; init; }

        [Range(0, 80, ErrorMessage = "Years of experience must be between 0 and 80.")]
        public int? YearsOfExperience { get; init; }
    }
}
