using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CarePlus.API.Models.User
{
    [Table("appSetting")]
    public class AppSetting
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        [Required]
        public long UserId { get; set; }

        public User User { get; set; } = null!;

        [Required]
        [MaxLength(10)]
        public string Appearance { get; set; } = "light";

        [MaxLength(5)]
        public string? Language { get; set; }

        [Required]
        [MaxLength(10)]
        public string Notification { get; set; } = "all";

        [Required]
        public bool TwoFactorEnabled { get; set; } = false;

        [Required]
        public bool HasCompletedTutorial { get; set; } = false;
    }
}
