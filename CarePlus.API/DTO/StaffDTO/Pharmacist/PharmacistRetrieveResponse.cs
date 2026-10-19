namespace CarePlus.API.DTO.StaffDTO
{
    public class PharmacistRetrieveResponse
    {
        public string StaffId { get; set; } = string.Empty;
        public long UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string LicenseNumber { get; set; } = string.Empty;
        public string Qualification { get; set; } = string.Empty;
        public string? Certification { get; set; }
        public int YearsOfExperience { get; set; }
    }
}
