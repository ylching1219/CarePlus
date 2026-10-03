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
        public long Id { get; set; }

        [Required]
        [MaxLength(100)]
        public String Username { get; set; } = null!;

        public String? Name { get; set; } = null;

        public String? Email { get; set; }
        public String? Password { get; set;}



    }
}
