using CarePlus.API.Extension;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CarePlus.API.Models.Announcement;

[Table("ClinicAnnouncements")]
public class ClinicAnnouncement
{
    [Key]
    [MaxLength(80)]
    public string AnnouncementId { get; set; } = null!;

    [Required]
    public string Title { get; set; } = null!;

    [Required]
    public string Body { get; set; } = null!;

    [Required]
    public string TitleZh { get; set; } = null!;

    [Required]
    public string BodyZh { get; set; } = null!;

    public DateTime PublishedAt { get; set; } = DateTimeHelper.MalaysiaNow();

    public bool IsPublished { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTimeHelper.MalaysiaNow();
}
