using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace CarePlus.API.Models.User.Staff.Pharmacist
{
    [Tags("Pharmacists")]
    [Index(nameof(PharmacistId), IsUnique = true)]
    public class Pharmacist : Staff
    {
        [Required]
        public string PharmacistId { get; set; } = null!;
    }
}
