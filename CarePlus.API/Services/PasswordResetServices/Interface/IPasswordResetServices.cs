using CarePlus.API.DTO.UserDTO;

namespace CarePlus.API.Services.PasswordResetServices.Interface;

public interface IPasswordResetServices
{
    Task<ApiResponse<object>> RequestCodeAsync(PasswordResetRequestCodeRequest request, CancellationToken cancellationToken);
    Task<ApiResponse<object>> VerifyCodeAsync(PasswordResetVerifyCodeRequest request, CancellationToken cancellationToken);
    Task<ApiResponse<object>> ResetPasswordAsync(PasswordResetCompleteRequest request, CancellationToken cancellationToken);
}
