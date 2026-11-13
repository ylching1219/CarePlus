using CarePlus.API.DTO.UserDTO;

namespace CarePlus.API.Services.EmailVerificationServices.Interface;

public interface IEmailVerificationServices
{
    Task<ApiResponse<object>> SendCodeAsync(long userId);
    Task<ApiResponse<object>> ResendCodeAsync(string challengeId);
    Task<ApiResponse<object>> VerifyCodeAsync(EmailVerificationRequest request);
}
