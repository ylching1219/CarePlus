using System.ComponentModel.DataAnnotations;

namespace CarePlus.API.DTO.UserDTO;

public sealed class PasswordResetRequestCodeRequest
{
    [Required]
    [EmailAddress(ErrorMessage = "Enter a valid email address.")]
    [MaxLength(254)]
    public string Email { get; set; } = string.Empty;
}

public sealed class PasswordResetVerifyCodeRequest
{
    [Required]
    [MaxLength(64)]
    public string ChallengeId { get; set; } = string.Empty;

    [Required]
    [RegularExpression("^\\d{6}$", ErrorMessage = "Enter the 6-digit verification code.")]
    public string Code { get; set; } = string.Empty;
}

public sealed class PasswordResetCompleteRequest
{
    [Required]
    [MaxLength(64)]
    public string ChallengeId { get; set; } = string.Empty;

    [Required]
    [MaxLength(128)]
    public string ResetToken { get; set; } = string.Empty;

    [Required]
    [StringLength(72, MinimumLength = 8)]
    public string NewPassword { get; set; } = string.Empty;
}

public sealed class PasswordResetCodeSentResponse
{
    public string ChallengeId { get; set; } = string.Empty;
    public string MaskedEmail { get; set; } = string.Empty;
    public int ExpiresInSeconds { get; set; }
}

public sealed class PasswordResetCodeVerifiedResponse
{
    public string ChallengeId { get; set; } = string.Empty;
    public string ResetToken { get; set; } = string.Empty;
    public int ExpiresInSeconds { get; set; }
}

public sealed class PasswordResetCompletedResponse
{
    public bool Reset { get; set; }
}
