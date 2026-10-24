using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CarePlus.API.Models.User
{
    [Table("LoginActivities")]
    public class LoginActivity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        [Required]
        public long UserId { get; set; }

        public User User { get; set; } = null!;

        [Required]
        [MaxLength(120)]
        public string Device { get; set; } = string.Empty;

        [Required]
        [MaxLength(64)]
        public string IpAddress { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }
    }
}
