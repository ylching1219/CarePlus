using CarePlus.API.DTO.UserDTO;

namespace CarePlus.API.DTO.PatientDTO
{
    public class PatientRegisterResponse : UserRegisterResponse
    {
        public string PatientId { get; set; } = string.Empty;
    }
}
