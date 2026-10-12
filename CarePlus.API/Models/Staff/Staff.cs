using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CarePlus.API.Models.User.Staff
{
    [Table("Staffs")]
    public class Staff
    {
        [Key]
        [Required]
        public string StaffId { get; set; } = null!;

        [Required]
        public long UserId { get; set; }

        public User User { get; set; } = null!;

        [Required]
        public string Role { get; set; } = null!;
    }
}
