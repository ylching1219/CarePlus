namespace CarePlus.API.DTO.UserDTO
{
    public class UserRetrieveResponse
    {
        public long userId { get; set; }
        public string username { get; set; } = string.Empty;
        public string name { get; set; } = string.Empty;
        public string email { get; set; } = string.Empty;
        public string phone { get; set; } = string.Empty;
        public string status { get; set; } = string.Empty;

    }
}
