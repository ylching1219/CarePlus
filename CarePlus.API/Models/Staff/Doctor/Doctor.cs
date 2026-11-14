using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CarePlus.API.Models.User.Staff.Doctor
{
    [Table("Doctors")]
    [Index(nameof(LicenseNumber), IsUnique = true)]
    public class Doctor : Staff
    {
        [Required, StringLength(50)]
        public string LicenseNumber { get; set; } = null!;

        [Required, StringLength(100)]
        public string Specialization { get; set; } = null!;

        [Required, StringLength(150)]
        public string Qualification { get; set; } = null!;

        [Range(0, 80)]
        public int YearsOfExperience { get; set; }
    }
}
