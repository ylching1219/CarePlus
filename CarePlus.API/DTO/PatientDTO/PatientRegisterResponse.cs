using CarePlus.API.DTO.UserDTO;

namespace CarePlus.API.DTO.PatientDTO
{
    public class PatientRegisterResponse : UserRegisterResponse
    {
        public string PatientId { get; set; } = string.Empty;
        public bool EmailVerificationRequired { get; set; }
        public string VerificationChallengeId { get; set; } = string.Empty;
        public string MaskedEmail { get; set; } = string.Empty;
        public int VerificationExpiresInSeconds { get; set; }
    }
}
