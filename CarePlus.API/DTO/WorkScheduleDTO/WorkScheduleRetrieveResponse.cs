namespace CarePlus.API.DTO.WorkScheduleDTO
{
    public class WorkScheduleRetrieveResponse
    {
        public string ScheduleId { get; set; } = string.Empty;
        public string StaffId { get; set; } = string.Empty;
        public string StaffName { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public DateOnly WorkingDate { get; set; }
        public string Shift { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
    }
}
