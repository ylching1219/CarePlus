using CarePlus.API.Extension;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CarePlus.API.Models.FamilyGroup
{
    [Table("FamilyGroups")]
    public class FamilyGroup
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long FamilyGroupId { get; set; }

        [Required]
        [MaxLength(100)]
        public string GroupName { get; set; } = null!;

        public int MemberCount { get; set; } = 1;

        [Required]
        public string CreatedBy { get; set; } = null!;
        public Patient.Patient Patient { get; set; } = null!;

        public DateTime CreatedAt { get; set; } = DateTimeHelper.MalaysiaNow();
    }
}
