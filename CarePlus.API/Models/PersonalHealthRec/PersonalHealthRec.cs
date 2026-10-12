using CarePlus.API.Extension;
using CarePlus.API.Models.User.Patient;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CarePlus.API.Models.PersonalHealthRec
{
    [Table("PersonalHealthRecords")]
    public class PersonalHealthRec
    {
        [Key]
        public string HealthRecId { get; set; } = null!;

        [Required]
        public string PatientId { get; set; } = null!;
        public Patient Patient { get; set; } = null!;

        [Required]
        public string RecordType { get; set; } = null!;
        public string? BloodPressure { get; set; }
        public string? Allergies { get; set; }
        public string? HealthConditions { get; set; }
        public string? MedicalHistory { get; set; }
        public string? Symptoms { get; set; } 
        public string? HealthNotes { get; set; }
        public DateTime CreatedAt { get; set; } = DateTimeHelper.MalaysiaNow();

    }
}
