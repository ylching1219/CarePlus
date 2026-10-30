using CarePlus.API.Extension;
using CarePlus.API.Models.User.Staff;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CarePlus.API.Models.Leave
{
    [Table("LeaveApplications")]
    public class Leave
    {
        [Key]
        public string LeaveId { get; set; } = null!;

        [Required]
        public string StaffId { get; set; } = null!;
        public Staff Staff { get; set; } = null!;

        [Required]
        public DateTime StartDate { get; set; }

        [Required]
        public DateTime EndDate { get; set; }

        [Required]
        public string Reason { get; set; } = null!;

        [Required]
        [MaxLength(500)]
        public string SupportDocument { get; set; } = null!;

        public DateTime CreatedAt { get; set; } = DateTimeHelper.MalaysiaNow();

        public string Status { get; set; } = "Pending";
    }
}
