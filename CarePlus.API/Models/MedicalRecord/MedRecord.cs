
using CarePlus.API.Models.User.Staff.Doctor;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CarePlus.API.Models.MedicalRecord
{
    [Table("MedicalRecords")]
    public class MedRecord
    {
        [Key]
        public string MedicalRecordId { get; set; } = null!;

        [Required]
        public string PatientId { get; set; } = null!;
        public Patient.Patient Patient { get; set; } = null!;

        [Required]
        public string DoctorId { get; set; } = null!;
        public Doctor Doctor { get; set; } = null!;

        [Required]
        public DateTime ConsultationDate { get; set; }

        [Required]
        public string Diagnosis { get; set; } = null!;

        [Required]
        public string Treatment { get; set; } = null!;

        public string? Notes { get; set; }  

    }
}
