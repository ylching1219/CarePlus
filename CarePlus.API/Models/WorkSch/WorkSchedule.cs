using CarePlus.API.Models.User.Staff;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CarePlus.API.Models.WorkSch
{
    [Table("WorkingSchedules")]
    public class WorkSchedule
    {
        [Key]
        public string ScheduleId { get; set; } = null!;
        public string StaffId { get; set; } = null!;
        public Staff Staff { get; set; } = null!;

        [Required]
        public DateOnly WorkingDate { get; set; }

        [Required]
        public TimeOnly StartTime { get; set; }

        [Required]
        public TimeOnly EndTime { get; set; }

        public string Status { get; set; } = "Active";
    }
}
