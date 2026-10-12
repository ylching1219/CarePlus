using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CarePlus.API.Models.User.Staff.Pharmacist
{
    [Table("Pharmacists")]
    [Index(nameof(LicenseNumber), IsUnique = true)]
    public class Pharmacist : Staff
    {
        [Required, StringLength(50)]
        public string LicenseNumber { get; set; } = null!;

        [Required, StringLength(150)]
        public string Qualification { get; set; } = null!;

        [StringLength(150)]
        public string? Certification { get; set; }

        [Range(0, 80)]
        public int YearsOfExperience { get; set; }
    }
}
