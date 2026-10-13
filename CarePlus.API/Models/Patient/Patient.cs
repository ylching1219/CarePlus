using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CarePlus.API.Models.Patient
{
    [Table("Patients")]
    public class Patient
    {
        [Key]
        public string PatientId { get; set; } = null!;

        [Required]
        public long UserId { get; set; }

        public User.User User { get; set; } = null!;

        public DateTime? DateOfBirth { get; set; }

        [Required]
        public char Gender { get; set; }

        public string? Address { get; set; }
    }
}
