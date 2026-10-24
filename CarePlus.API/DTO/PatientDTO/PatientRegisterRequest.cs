using CarePlus.API.DTO.UserDTO;
using System.ComponentModel.DataAnnotations;

namespace CarePlus.API.DTO.PatientDTO
{
    public record PatientRegisterRequest : UserRegisterRequest
    {
        [Required(ErrorMessage = "Identity card number is required.")]
        public string IdentityCardNumber { get; init; } = string.Empty;

    }
}
