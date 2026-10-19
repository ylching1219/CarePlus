using CarePlus.API.DTO.UserDTO;

namespace CarePlus.API.Services.UserServices.Interface
{
    public interface IUserServices
    {
        Task<ApiResponse<object>> RegisterUserAsync(UserRegisterRequest request);

        Task<ApiResponse<object>> UserLoginAsync(UserLoginRequest request);
        Task<ApiResponse<object>> RetrieveAllUserAsync();

        Task<ApiResponse<object>> RetrieveUserByIdAsync(long id);

        Task<ApiResponse<object>> RetrieveAllActiveUserAsync();

        Task<ApiResponse<object>> UpdateUserAsync(UserUpdateRequest request);

        Task<List<string>> ValidateUserAsync(UserRegisterRequest request);
    }
}
