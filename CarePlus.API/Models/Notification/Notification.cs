
using CarePlus.API.Extension;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CarePlus.API.Models.Notification
{
    [Table("Notifications")]
    public class Notification
    {
        [Key]
        public string NotificationId { get; set; } = null!;

        // Null means this is the public broadcast copy of a clinic announcement.
        // User-specific copies have a recipient and their own read status.
        public long? UserId { get; set; }
        public User.User? User { get; set; }

        [Required]
        public string Title { get; set; } = null!;

        [Required]
        public string Message { get; set; } = null!;

        [Required]
        public string Type { get; set; } = null!; 

        [MaxLength(255)]
        public string Actor { get; set; } = "CarePlus Clinic";

        [MaxLength(8)]
        public string Initials { get; set; } = "CC";

        public string? TitleZh { get; set; }

        public string? MessageZh { get; set; }

        [MaxLength(500)]
        public string Href { get; set; } = "/notifications";

        public string Status { get; set; } = "Unread";

        public bool IsPublished { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTimeHelper.MalaysiaNow();
    }
}
