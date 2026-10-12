using CarePlus.API.Extension;
using CarePlus.API.Models.MedicalRecord;
using CarePlus.API.Models.User.Patient;
using CarePlus.API.Models.User.Staff.Doctor;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CarePlus.API.Models.Prescription
{
    [Table("Prescriptions")]
    public class Prescription
    {
        [Key]
        public string PrescriptionId { get; set; } = null!;

        [Required]
        public string PatientId { get; set; } = null!;
        public Patient Patient { get; set; } = null!;

        [Required]
        public string DoctorId { get; set; } = null!;
        public Doctor Doctor { get; set; } = null!;

        [Required]
        public string MedicalRecordId { get; set; } = null!;
        public MedRecord MedicalRecord { get; set; } = null!;

        public DateTime CreatedAt { get; set; } = DateTimeHelper.MalaysiaNow();

        public string Status { get; set; } = "Pending"; 
    }
}
