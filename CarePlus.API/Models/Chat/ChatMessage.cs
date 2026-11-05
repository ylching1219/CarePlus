using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CarePlus.API.Models.Chat;

[Table("ChatMessages")]
public sealed class ChatMessage
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long ChatMessageId { get; set; }

    public long ChatThreadId { get; set; }

    public ChatThread ChatThread { get; set; } = null!;

    [Required]
    [MaxLength(16)]
    public string Role { get; set; } = null!;

    [Required]
    public string Content { get; set; } = null!;

    [MaxLength(1000)]
    public string? Disclaimer { get; set; }

    public string? SourcesJson { get; set; }

    public DateTime CreatedAt { get; set; }
}
