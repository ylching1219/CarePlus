namespace CarePlus.API.DTO.UserDTO
{
    public class ProfileUpdateRequest
    {
        public string? Name { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? Gender { get; set; }
        public string? Address { get; set; }
    }
}
