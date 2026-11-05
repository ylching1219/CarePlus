using CarePlus.API.DTO.UserDTO;
using System.ComponentModel.DataAnnotations;

namespace CarePlus.API.DTO.PatientDTO
{
    public record PatientRegisterRequest : UserRegisterRequest
    {
        [Required(ErrorMessage = "Identity card number is required.")]
        public string IdentityCardNumber { get; init; } = string.Empty;

        [Required(ErrorMessage = "Verify your email address before registering.")]
        public string EmailVerificationChallengeId { get; init; } = string.Empty;

        [Required(ErrorMessage = "Verify your email address before registering.")]
        public string EmailVerificationToken { get; init; } = string.Empty;

    }
}
