using CarePlus.API.DTO.UserDTO;

namespace CarePlus.API.Services.TwoFactorServices.Interface;

public interface ITwoFactorServices
{
    Task<ApiResponse<object>> SendCodeAsync(long userId, TwoFactorSendCodeRequest request);
    Task<ApiResponse<object>> VerifyCodeAsync(long userId, TwoFactorVerifyRequest request);
}
