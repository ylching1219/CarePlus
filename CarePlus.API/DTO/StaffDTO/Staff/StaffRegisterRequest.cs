using CarePlus.API.DTO.UserDTO;

namespace CarePlus.API.DTO.StaffDTO
{
    public record StaffRegisterRequest : UserRegisterRequest
    {
        public string Role { get; set; } = string.Empty;
    }
}
