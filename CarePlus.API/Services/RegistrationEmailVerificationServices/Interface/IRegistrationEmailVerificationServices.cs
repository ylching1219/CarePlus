using CarePlus.API.DTO.UserDTO;

namespace CarePlus.API.Services.RegistrationEmailVerificationServices.Interface;

public interface IRegistrationEmailVerificationServices
{
    Task<ApiResponse<object>> RequestCodeAsync(RegistrationEmailCodeRequest request, CancellationToken cancellationToken);
    Task<ApiResponse<object>> VerifyCodeAsync(RegistrationEmailCodeVerifyRequest request, CancellationToken cancellationToken);
    Task<bool> IsVerifiedAsync(string email, string challengeId, string verificationToken, CancellationToken cancellationToken = default);
    Task ConsumeAsync(string challengeId, CancellationToken cancellationToken = default);
}
