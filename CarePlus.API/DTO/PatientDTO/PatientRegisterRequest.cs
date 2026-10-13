using CarePlus.API.DTO.UserDTO;
using System.ComponentModel.DataAnnotations;

namespace CarePlus.API.DTO.PatientDTO
{
    public record PatientRegisterRequest : UserRegisterRequest
    {
       
        [Required(ErrorMessage = "Gender is required.")]
        public string Gender {  get; init; } = string.Empty;

    }
}
