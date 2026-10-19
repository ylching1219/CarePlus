using CarePlus.API.DTO.UserDTO;
using System.ComponentModel.DataAnnotations;

namespace CarePlus.API.DTO.StaffDTO
{
    public record PharmacistRegisterRequest : UserRegisterRequest
    {
        [Required(ErrorMessage = "License number is required.")]
        [StringLength(50)]
        public string LicenseNumber { get; init; } = string.Empty;

        [Required(ErrorMessage = "Qualification is required.")]
        [StringLength(150)]
        public string Qualification { get; init; } = string.Empty;

        [StringLength(150)]
        public string? Certification { get; init; }

        [Range(0, 80, ErrorMessage = "Years of experience must be between 0 and 80.")]
        public int YearsOfExperience { get; init; }
    }
}
