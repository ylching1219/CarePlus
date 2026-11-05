using System.ComponentModel.DataAnnotations;

namespace CarePlus.API.DTO.UserDTO;

public sealed class RegistrationEmailCodeRequest
{
    [Required]
    [EmailAddress(ErrorMessage = "Enter a valid email address.")]
    [MaxLength(254)]
    public string Email { get; set; } = string.Empty;
}

public sealed class RegistrationEmailCodeVerifyRequest
{
    [Required]
    [MaxLength(64)]
    public string ChallengeId { get; set; } = string.Empty;

    [Required]
    [RegularExpression("^\\d{6}$", ErrorMessage = "Enter the 6-digit verification code.")]
    public string Code { get; set; } = string.Empty;
}

public sealed class RegistrationEmailCodeSentResponse
{
    public string ChallengeId { get; set; } = string.Empty;
    public string MaskedEmail { get; set; } = string.Empty;
    public int ExpiresInSeconds { get; set; }
}

public sealed class RegistrationEmailCodeVerifiedResponse
{
    public string ChallengeId { get; set; } = string.Empty;
    public string VerificationToken { get; set; } = string.Empty;
    public int ExpiresInSeconds { get; set; }
}
