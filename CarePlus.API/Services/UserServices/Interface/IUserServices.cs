using CarePlus.API.DTO.DeliveryAddressDTO;
using CarePlus.API.DTO.PaymentCardDTO;
using CarePlus.API.DTO.UserDTO;

namespace CarePlus.API.Services.UserServices.Interface
{
    public interface IUserServices
    {
        Task<ApiResponse<object>> RegisterUserAsync(UserRegisterRequest request, bool emailVerified = true);

        Task<ApiResponse<object>> CheckUsernameAvailabilityAsync(string username);

        Task EnsureAppSettingAsync(long userId, bool staff);

        Task<ApiResponse<object>> PatientLoginAsync(UserLoginRequest request);

        Task<ApiResponse<object>> StaffLoginAsync(UserLoginRequest request);
        Task<ApiResponse<object>> RetrieveAllUserAsync();

        Task<ApiResponse<object>> RetrieveUserByIdAsync(long id);

        Task<ApiResponse<object>> RetrieveCurrentUserAsync(long id);

        Task<ApiResponse<object>> RetrieveLoginActivityAsync(long userId);

        Task<ApiResponse<object>> RetrieveAllActiveUserAsync();

        Task<ApiResponse<object>> UpdateUserAsync(UserUpdateRequest request);

        Task<ApiResponse<object>> UpdateCurrentProfileAsync(long userId, ProfileUpdateRequest request);

        Task<ApiResponse<object>> ChangePasswordAsync(long userId, PasswordChangeRequest request);

        Task<ApiResponse<object>> DeleteCurrentAccountAsync(long userId, AccountDeleteRequest request);

        Task<ApiResponse<object>> RetrieveDeliveryAddressesAsync(long userId);

        Task<ApiResponse<object>> AddDeliveryAddressAsync(long userId, DeliveryAddressSaveRequest request);

        Task<ApiResponse<object>> UpdateDeliveryAddressAsync(long userId, long addressId, DeliveryAddressSaveRequest request);

        Task<ApiResponse<object>> DeleteDeliveryAddressAsync(long userId, long addressId);

        Task<ApiResponse<object>> SetDefaultDeliveryAddressAsync(long userId, long? addressId);

        Task<ApiResponse<object>> SaveProfilePictureAsync(long userId, IFormFile? file);

        Task<ApiResponse<object>> RetrieveAppSettingAsync(long userId);

        Task<ApiResponse<object>> SaveAppSettingAsync(long userId, AppSettingSaveRequest request);

        Task<ApiResponse<object>> RetrievePaymentCardsAsync(long userId);

        Task<ApiResponse<object>> AddPaymentCardAsync(long userId, PaymentCardSaveRequest request);

        Task<ApiResponse<object>> UpdatePaymentCardAsync(long userId, long cardId, PaymentCardSaveRequest request);

        Task<ApiResponse<object>> DeletePaymentCardAsync(long userId, long cardId);

        Task<ApiResponse<object>> SetDefaultPaymentCardAsync(long userId, long cardId);

        Task<List<string>> ValidateUserAsync(UserRegisterRequest request);
    }
}
