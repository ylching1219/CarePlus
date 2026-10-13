using CarePlus.API.DTO.UserDTO;

namespace CarePlus.API.Services.UserServices.Interface
{
    public interface IUserServices
    {
        Task<ApiResponse<object>> RegisterUserAsync(UserRegisterRequest request);

        Task<ApiResponse<object>> RetreiveAllUserAsync();

        Task<ApiResponse<object>> RetreiveUserByIdAsync(long id);

        Task<ApiResponse<object>> RetrieveAllActiveUserAsync();

        Task<ApiResponse<object>> UpdateUserAsync(UserUpdateRequest request);
    }
}
