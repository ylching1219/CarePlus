using CarePlus.API.DTO.UserDTO;

namespace CarePlus.API.DTO.PatientDTO
{
    public class PatientRetrieveResponse : UserRetrieveResponse
    {
        public string PatientId { get; set; } = string.Empty;
        public DateTime? DateOfBirth { get; set; }

        public string Gender { get; set; } = string.Empty;

        public string Address { get; set; } = string.Empty;
    }
}
