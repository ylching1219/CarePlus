using CarePlus.API.Models.Common;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CarePlus.API.Models.User
{
    [Table("Users")]
    [Index(nameof(Username), IsUnique = true)]
    public class User : AuditTrail
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long UserId { get; set; }

        [Required]
        [MaxLength(100)]
        public string Username { get; set; } = null!;

        public string? Name { get; set; } = null;

        [Required]
        public string Email { get; set; } = null!;

        [Required]
        public string? PasswordHash { get; set;}
        public long? Phone { get; set; }
        public string? Status { get; set; } = "Active";



    }
}
