namespace CarePlus.API.DTO.AnnouncementDTO;

using System.ComponentModel.DataAnnotations;

public class AnnouncementCreateRequest
{
    [Required]
    [StringLength(160, MinimumLength = 3)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [StringLength(4000, MinimumLength = 3)]
    public string Body { get; set; } = string.Empty;

    [StringLength(160)]
    public string? TitleZh { get; set; }

    [StringLength(4000)]
    public string? BodyZh { get; set; }
}

public class AnnouncementResponse
{
    public string Id { get; set; } = "";
    public string PostedOn { get; set; } = "";
    public string Title { get; set; } = "";
    public string Body { get; set; } = "";
    public string TitleZh { get; set; } = "";
    public string BodyZh { get; set; } = "";
}
