using CarePlus.API.DTO.UserDTO;

namespace CarePlus.API.DTO.PatientDTO
{
    public class PatientRegisterResponse : UserRegisterResponse
    {
        public string patientId { get; set; } = string.Empty;
    }
}
