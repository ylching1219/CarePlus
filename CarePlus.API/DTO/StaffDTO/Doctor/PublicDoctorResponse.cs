namespace CarePlus.API.DTO.StaffDTO
{
    public class PublicDoctorResponse
    {
        public string StaffId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Specialization { get; set; } = string.Empty;
        public string Qualification { get; set; } = string.Empty;
        public int YearsOfExperience { get; set; }
        public List<PublicDoctorScheduleResponse> Schedules { get; set; } = [];
    }

    public class PublicDoctorScheduleResponse
    {
        public DateTime WorkingDate { get; set; }
        public string StartTime { get; set; } = string.Empty;
        public string EndTime { get; set; } = string.Empty;
    }
}
