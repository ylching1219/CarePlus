using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace CarePlus.API.Models.User;

[Table("RegistrationEmailChallenges")]
[Index(nameof(Email), nameof(CreatedAt))]
public sealed class RegistrationEmailChallenge
{
    [Key]
    [MaxLength(64)]
    public string ChallengeId { get; set; } = string.Empty;

    [Required]
    [MaxLength(254)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MaxLength(128)]
    public string SecretHash { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime? VerifiedAt { get; set; }
    public DateTime? ConsumedAt { get; set; }
    public int Attempts { get; set; }
}
