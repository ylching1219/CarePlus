namespace CarePlus.API.DTO.UserDTO
{
    public class LoginActivityResponse
    {
        public long Id { get; set; }
        public string Device { get; set; } = string.Empty;
        public string IpAddress { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}
