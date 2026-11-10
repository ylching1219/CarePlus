
using CarePlus.API.Models.Room;
using CarePlus.API.Models.User.Staff.Doctor;
using CarePlus.API.Models.WorkSch;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CarePlus.API.Models.RoomAssg
{
    [Table("RoomAssignments")]
    public class RoomAssignment
    {
        [Key]
        public string AssignmentId { get; set; } = null!;

        [Required]
        public string RoomId { get; set; } = null!;
        public ClinicRoom Room { get; set; } = null!;

        public string? ScheduleId { get; set; }
        public WorkSchedule? Schedule { get; set; }

        [Required]
        public string DoctorId { get; set; } = null!;
        public Doctor Doctor { get; set; } = null!;

        [Required]
        public DateOnly AssignmentDate { get; set; }

        [Required]
        public TimeOnly StartTime { get; set; }

        [Required]
        public TimeOnly EndTime { get; set; }

    }
}
