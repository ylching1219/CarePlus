using CarePlus.API.DTO.UserDTO;

namespace CarePlus.API.Services.UserServices.Interface
{
    public interface IUserServices
    {
        Task<ApiResponse<Object>> RegisterUserAsync(UserRegisterRequest request);

        Task<ApiResponse<Object>> RetreiveAllUserAsync();

        Task<ApiResponse<Object>> RetreiveUserByIdAsync(int id); 
    }
}
