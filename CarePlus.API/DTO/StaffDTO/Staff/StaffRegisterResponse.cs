namespace CarePlus.API.DTO.StaffDTO
{
    public class StaffRegisterResponse
    {
        public string StaffId { get; set; } = string.Empty;

        public long UserId { get; set; }
        public string Name { get; set; } = string.Empty;

    }
}
