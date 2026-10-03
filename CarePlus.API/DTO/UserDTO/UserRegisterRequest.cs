using System.ComponentModel.DataAnnotations;

namespace CarePlus.API.DTO.UserDTO
{
    public record UserRegisterRequest
    {
        [Required(ErrorMessage = "Username is required.")]
        public string Username { get; init; } = string.Empty;

        [Required(ErrorMessage = "Name is required.")]
        [StringLength(100, MinimumLength = 3 , ErrorMessage = "Name cannot exceed 100 characters.")]
        public string Name { get; init; } = string.Empty;

        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Invalid email format.")]
        public string Email { get; init; } = string.Empty;

        [Required(ErrorMessage = "Password is required.")]
        [StringLength(100, MinimumLength = 8, ErrorMessage = "Password must be at least 8 characters long.")]
        public string Password { get; init; } = string.Empty;
    }
}
