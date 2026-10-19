namespace CarePlus.API.DTO.StaffDTO
{
    public class DoctorRegisterResponse
    {
        public string StaffId { get; set; } = string.Empty;
        public long UserId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string LicenseNumber { get; set; } = string.Empty;
        public string Specialization { get; set; } = string.Empty;
        public string Qualification { get; set; } = string.Empty;
        public int YearsOfExperience { get; set; }
    }
}
