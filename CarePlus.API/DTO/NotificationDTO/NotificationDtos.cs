namespace CarePlus.API.DTO.NotificationDTO;

public class NotificationResponse
{
    public string Id { get; set; } = "";
    public string Title { get; set; } = "";
    public string Message { get; set; } = "";
    public string TitleZh { get; set; } = "";
    public string MessageZh { get; set; } = "";
    public string Actor { get; set; } = "";
    public string Initials { get; set; } = "";
    public string Type { get; set; } = "";
    public string Href { get; set; } = "";
    public string CreatedAt { get; set; } = "";
    public bool Unread { get; set; }
}

public class NotificationIdsRequest
{
    public List<string> Ids { get; set; } = [];
}
