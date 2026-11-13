using CarePlus.API.DTO.UserDTO;
using CarePlus.API.Models.User;

namespace CarePlus.API.Services.TwoFactorServices.Interface;

public interface ITwoFactorServices
{
    Task<ApiResponse<object>> SendCodeAsync(long userId, TwoFactorSendCodeRequest request);
    Task<ApiResponse<object>> VerifyCodeAsync(long userId, TwoFactorVerifyRequest request);
    Task<ApiResponse<object>> StartLoginChallengeAsync(User user);
    Task<ApiResponse<object>> ResendLoginChallengeAsync(string challengeId);
    Task<ApiResponse<long>> ConsumeLoginChallengeAsync(string challengeId, string code);
}
