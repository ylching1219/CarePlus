using System.ComponentModel.DataAnnotations;

namespace CarePlus.API.DTO.UserDTO
{
    public class PasswordChangeRequest
    {
        [Required(ErrorMessage = "Enter your current password.")]
        public string CurrentPassword { get; init; } = string.Empty;

        [Required(ErrorMessage = "Enter a new password.")]
        public string NewPassword { get; init; } = string.Empty;
    }
}
