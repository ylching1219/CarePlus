using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CarePlus.API.Models.User;

[Table("TwoFactorChallenges")]
public class TwoFactorChallenge
{
    [Key]
    [MaxLength(64)]
    public string ChallengeId { get; set; } = string.Empty;

    [Required]
    public long UserId { get; set; }

    public User User { get; set; } = null!;

    [Required]
    [MaxLength(10)]
    public string Purpose { get; set; } = string.Empty;

    [Required]
    [MaxLength(128)]
    public string CodeHash { get; set; } = string.Empty;

    [Required]
    public DateTime CreatedAt { get; set; }

    [Required]
    public DateTime ExpiresAt { get; set; }

    public DateTime? ConsumedAt { get; set; }

    [Required]
    public int Attempts { get; set; }
}
