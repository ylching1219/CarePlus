using CarePlus.API.Models.User.Staff.Doctor;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CarePlus.API.Models.Appointment
{
    [Table("Appointments")]
    public class Appointment
    {
        [Key]
        public string AppointmentId { get; set; } = null!;

        [Required]
        public string PatientId { get; set; } = null!;
        public Patient.Patient Patient { get; set; } = null!;

        [Required]
        public string DoctorId { get; set; } = null!;
        public Doctor Doctor { get; set; } = null!;

        [Required]
        public DateOnly AppointmentDate { get; set; }

        [Required]
        public TimeOnly AppointmentTime { get; set; }

        public string Status { get; set; } = "Scheduled"; 

        public string CheckInStatus { get; set; } = "Pending";
    }
}
