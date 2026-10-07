using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace CarePlus.API.Models.User.Patient
{
    [Tags("Patients")]
    [Index(nameof(PatientId), IsUnique = true)]
    public class Patient : User
    {
    
        public string PatientId { get; set; } = null!;

        public DateTime? DateOfBirth { get; set; }

        [Required]
        public char Gender { get; set; }

        public string? Address { get; set; }
    }
}
