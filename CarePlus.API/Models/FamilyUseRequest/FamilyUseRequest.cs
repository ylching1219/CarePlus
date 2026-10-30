using CarePlus.API.Extension;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using FamilyGroupModel = CarePlus.API.Models.FamilyGroup.FamilyGroup;

namespace CarePlus.API.Models.FamilyUseRequest;

[Table("FamilyUseRequests")]
public class FamilyUseRequest
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long FamilyUseRequestId { get; set; }

    [Required]
    public long FamilyGroupId { get; set; }
    public FamilyGroupModel FamilyGroup { get; set; } = null!;

    [Required]
    public string FromPatientId { get; set; } = null!;
    public Patient.Patient FromPatient { get; set; } = null!;

    [Required]
    public string TargetPatientId { get; set; } = null!;
    public Patient.Patient TargetPatient { get; set; } = null!;

    [Required]
    [MaxLength(20)]
    public string Status { get; set; } = "Pending";

    public DateTime CreatedAt { get; set; } = DateTimeHelper.MalaysiaNow();
    public DateTime? DecidedAt { get; set; }
}
