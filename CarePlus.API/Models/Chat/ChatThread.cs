using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CarePlus.API.Models.Chat;

[Table("ChatThreads")]
public sealed class ChatThread
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long ChatThreadId { get; set; }

    [Required]
    [MaxLength(20)]
    public string PatientId { get; set; } = null!;

    public CarePlus.API.Models.Patient.Patient Patient { get; set; } = null!;

    [Required]
    [MaxLength(120)]
    public string Title { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public ICollection<ChatMessage> Messages { get; set; } = new List<ChatMessage>();
}
