using System.ComponentModel.DataAnnotations;

namespace CarePlus.API.DTO.UserDTO;

public class EmailVerificationRequest
{
    [Required]
    public string ChallengeId { get; set; } = string.Empty;

    [Required]
    [RegularExpression("^\\d{6}$", ErrorMessage = "Enter the 6-digit verification code.")]
    public string Code { get; set; } = string.Empty;
}

public class EmailVerificationResendRequest
{
    [Required]
    public string ChallengeId { get; set; } = string.Empty;
}

public class EmailVerificationCodeSentResponse
{
    public string ChallengeId { get; set; } = string.Empty;
    public string MaskedEmail { get; set; } = string.Empty;
    public int ExpiresInSeconds { get; set; }
}

public class EmailVerificationStatusResponse
{
    public bool Verified { get; set; }
}
