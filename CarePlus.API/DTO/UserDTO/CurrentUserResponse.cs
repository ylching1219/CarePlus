namespace CarePlus.API.DTO.UserDTO
{
    public class CurrentUserResponse : UserRetrieveResponse
    {
        public string AccountType { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string? PatientId { get; set; }
        public string? StaffId { get; set; }
    }
}
