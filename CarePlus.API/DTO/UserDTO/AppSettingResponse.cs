namespace CarePlus.API.DTO.UserDTO
{
    public class AppSettingResponse
    {
        public string Appearance { get; set; } = "light";
        public string? Language { get; set; }
        public string Notification { get; set; } = "all";
    }
}
