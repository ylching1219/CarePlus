using CarePlus.API.Extension;
using CarePlus.API.Models.User.Staff.Pharmacist;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CarePlus.API.Models.DispenseRec
{
    [Table("DispensingRecords")]
    public class DispenseRec
    {
        [Key]
        public string DispensingId { get; set; } = null!;

        [Required]
        public string PrescriptionId { get; set; } = null!;
        public Prescription.Prescription Prescription { get; set; } = null!;

        [Required]
        public string PharmacistId { get; set; } = null!;
        public Pharmacist Pharmacist { get; set; } = null!;

        public DateTime DispenseAt { get; set; } = DateTimeHelper.MalaysiaNow();

        public string Status { get; set; } = "Dispensed"; 

    }
}
