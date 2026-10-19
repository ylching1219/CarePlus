namespace CarePlus.API.DTO.UserDTO
{
    public class UserRetrieveResponse
    {
        public long UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;

    }
}
