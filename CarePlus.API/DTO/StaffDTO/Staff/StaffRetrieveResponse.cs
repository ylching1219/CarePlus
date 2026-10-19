using CarePlus.API.DTO.UserDTO;

namespace CarePlus.API.DTO.StaffDTO
{
    public class StaffRetrieveResponse : UserRetrieveResponse
    {
        public string StaffId { get; set; } = string.Empty;

        public string Role { get; set; } = string.Empty;
    }
}
