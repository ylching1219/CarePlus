using System.ComponentModel.DataAnnotations;

namespace CarePlus.API.DTO.UserDTO;

public class TwoFactorSendCodeRequest
{
    [Required]
    public string Action { get; set; } = string.Empty;
}

public class TwoFactorVerifyRequest
{
    [Required]
    public string Action { get; set; } = string.Empty;

    [Required]
    [RegularExpression("^\\d{6}$", ErrorMessage = "Enter the 6-digit verification code.")]
    public string Code { get; set; } = string.Empty;
}

public class TwoFactorCodeSentResponse
{
    public string MaskedEmail { get; set; } = string.Empty;
    public int ExpiresInSeconds { get; set; }
}

public class TwoFactorStatusResponse
{
    public bool Enabled { get; set; }
}

public class LoginTwoFactorRequiredResponse
{
    public bool RequiresTwoFactor { get; set; } = true;
    public string ChallengeId { get; set; } = string.Empty;
    public string MaskedEmail { get; set; } = string.Empty;
    public int ExpiresInSeconds { get; set; }
}

public class LoginTwoFactorVerifyRequest
{
    [Required]
    public string ChallengeId { get; set; } = string.Empty;

    [Required]
    [RegularExpression("^\\d{6}$", ErrorMessage = "Enter the 6-digit verification code.")]
    public string Code { get; set; } = string.Empty;

    public bool RememberMe { get; set; }
}

public class LoginTwoFactorResendRequest
{
    [Required]
    public string ChallengeId { get; set; } = string.Empty;
}
