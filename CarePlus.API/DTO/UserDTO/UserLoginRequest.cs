using System.ComponentModel.DataAnnotations;

namespace CarePlus.API.DTO.UserDTO
{
    public record UserLoginRequest
    {
        [Required(ErrorMessage = "Username or email is required.")]
        public string Identifier { get; init; } = string.Empty;

        [Required(ErrorMessage = "Password is required.")]
        public string Password { get; init; } = string.Empty;
    }
}
