using System.ComponentModel.DataAnnotations;

namespace CarePlus.API.DTO.StaffDTO
{
    public record StaffPasswordUpdateRequest
    {
        [Required(ErrorMessage = "Staff Id is required.")]
        public string StaffId { get; init; } = string.Empty;

        [Required(ErrorMessage = "Enter a new password.")]
        public string NewPassword { get; init; } = string.Empty;
    }
}
