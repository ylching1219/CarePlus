namespace CarePlus.API.DTO.StaffDTO
{
    public class AdminRegisterResponse
    {
        public string StaffId { get; set; } = string.Empty;
        public long UserId { get; set; }
        public string Name { get; set; } = string.Empty;
        public int AccessLevel { get; set; }
        public string? Department { get; set; }
    }
}
