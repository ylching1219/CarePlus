using CarePlus.API.DTO.UserDTO;

namespace CarePlus.API.Services.UserServices.Interface
{
    public interface IUserServices
    {
        Task<ApiResponse<Object>> RegisterUserAsync(UserRegisterRequest request);
    }
}
