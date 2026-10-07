using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace CarePlus.API.Models.User.Staff.Doctor
{
    [Tags("Doctors")]
    [Index(nameof(DoctorId), IsUnique = true)]
    public class Doctor : Staff
    {
        [Required]
        public string DoctorId { get; set; } = null!;

    }
}
