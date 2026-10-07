using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace CarePlus.API.Models.User.Staff.Admin
{
    [Tags("Admins")]
    [Index(nameof(AdminId), IsUnique = true)]
    public class Admin : Staff
    {
        [Required]
        public string AdminId { get; set; } = null!;
        [Required]
        public int AccessLevel { get; set; }
    }
}
