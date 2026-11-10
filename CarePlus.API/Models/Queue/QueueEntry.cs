using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using CarePlus.API.Models.Room;

namespace CarePlus.API.Models.Queue
{
    [Table("QueueEntries")]
    public class QueueEntry
    {
        [Key]
        public string QueueId { get; set; } = null!;

        [Required]
        public string PatientId { get; set; } = null!;
        public Patient.Patient Patient { get; set; } = null!;

        
        public string? AppointmentId { get; set; }
        public Appointment.Appointment? Appointment { get; set; }

        public string? RoomId { get; set; }
        public ClinicRoom? Room { get; set; }

        [Required]
        public string QueueType { get; set; } = null!; 
        public string Status { get; set; } = "Waiting";
        public DateTime CheckInTime { get; set; } 
    }
}
