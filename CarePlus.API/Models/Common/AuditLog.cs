using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CarePlus.API.Models.Common
{
    [Table("AuditLogs")]
    public class AuditLog
    {
        [Key]
        [MaxLength(64)]
        public string LogId { get; set; } = null!;

        [Required]
        [MaxLength(100)]
        public string UserId { get; set; } = "SYSTEM";

        [Required]
        [MaxLength(20)]
        public string Action { get; set; } = null!;

        [Required]
        [MaxLength(100)]
        public string Module { get; set; } = null!;

        [Required]
        [MaxLength(255)]
        public string AffectedRecord { get; set; } = null!;

        [Required]
        public DateTime DateTime { get; set; }
    }
}
