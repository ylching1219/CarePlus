using CarePlus.API.Extension;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CarePlus.API.Models.FamilyMember
{
    [Table("FamilyMembers")]
    public class FamilyMember
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long FamilyMemberId { get; set; }

        [Required]
        public long FamilyGroupId { get; set; }
        public FamilyGroup.FamilyGroup FamilyGroup { get; set; } = null!;

        [Required]
        public string PatientId { get; set; } = null!;
        public Patient.Patient Patient { get; set; } = null!;

        [Required]
        public string Relationship { get; set; } = null!;

        [Required]
        public string AccessLevel { get; set; } = "View";

        public DateTime JoinAt { get; set; } = DateTimeHelper.MalaysiaNow();

        public string Status { get; set; } = "Pending";
    }
}
