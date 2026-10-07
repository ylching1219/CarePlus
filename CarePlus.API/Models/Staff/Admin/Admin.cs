using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CarePlus.API.Models.User.Staff.Admin
{
    [Table("Admins")]
    public class Admin : Staff
    {
        [Required]
        public int AccessLevel { get; set; }

        [StringLength(100)]
        public string? Department { get; set; }
    }
}
