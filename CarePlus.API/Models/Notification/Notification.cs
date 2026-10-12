
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

        [Required]
        public long UserId { get; set; }
        public User.User User { get; set; } = null!;

        [Required]
        public string Title { get; set; } = null!;

        [Required]
        public string Message { get; set; } = null!;

        [Required]
        public string Type { get; set; } = null!; 

        public string Status { get; set; } = "Unread";

        public DateTime CreatedAt { get; set; } = DateTimeHelper.MalaysiaNow();
    }
}
