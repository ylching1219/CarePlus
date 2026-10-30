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

        // ponytail: MySQL DATE/TIME come back as DateTime/TimeSpan; DateOnly/TimeOnly throw on materialize
        [Required]
        public DateTime AppointmentDate { get; set; }

        [Required]
        public TimeSpan AppointmentTime { get; set; }

        [StringLength(100)]
        public string ForName { get; set; } = "";

        [StringLength(80)]
        public string VisitType { get; set; } = "VT01";

        [StringLength(500)]
        public string? Notes { get; set; }

        public int RescheduleCount { get; set; }

        public DateTime? LastRescheduledAt { get; set; }

        public DateTime? CancelledAt { get; set; }

        public string Status { get; set; } = "Scheduled";

        public string CheckInStatus { get; set; } = "Pending";
    }
}
