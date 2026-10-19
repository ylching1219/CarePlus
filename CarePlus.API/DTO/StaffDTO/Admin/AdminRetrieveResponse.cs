namespace CarePlus.API.DTO.StaffDTO
{
    public class AdminRetrieveResponse
    {
        public string StaffId { get; set; } = string.Empty;
        public long UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public int AccessLevel { get; set; }
        public string? Department { get; set; }
    }
}
