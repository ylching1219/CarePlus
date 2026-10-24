namespace CarePlus.API.DTO.UserDTO
{
    public class CurrentUserResponse : UserRetrieveResponse
    {
        public string AccountType { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string? PatientId { get; set; }
        public string? StaffId { get; set; }
        public string? IdentityCardNumber { get; set; }
        public string? Gender { get; set; }
        public string? Address { get; set; }
        public string? ProfilePicture { get; set; }
    }
}
