using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace CarePlus.API.Models.User.Staff
{
    [Index(nameof(StaffId), IsUnique = true)]
    public class Staff : User
    {
        [Required]
        public string StaffId { get; set; } = null!;

        [Required]
        public string Role { get; set; } = null!;
    }
}
