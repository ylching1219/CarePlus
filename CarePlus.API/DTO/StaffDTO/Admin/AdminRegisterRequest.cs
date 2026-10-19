using CarePlus.API.DTO.UserDTO;
using System.ComponentModel.DataAnnotations;

namespace CarePlus.API.DTO.StaffDTO
{
    public record AdminRegisterRequest : UserRegisterRequest
    {
        [Range(1, 3, ErrorMessage = "Access level must be between 1 and 3.")]
        public int AccessLevel { get; init; }

        [StringLength(100, ErrorMessage = "Department cannot exceed 100 characters.")]
        public string? Department { get; init; }
    }
}
