using CarePlus.API.DTO.AnnouncementDTO;
using CarePlus.API.Extension;
using CarePlus.API.Models.Notification;
using CarePlus.API.Repository.Interface;
using CarePlus.API.Services.AnnouncementServices.Interface;

namespace CarePlus.API.Services.AnnouncementServices;

public class AnnouncementServices : IAnnouncementServices
{
    private readonly IAnnouncementRepository _announcementRepository;

    public AnnouncementServices(IAnnouncementRepository announcementRepository)
    {
        _announcementRepository = announcementRepository;
    }

    public async Task<ApiResponse<AnnouncementResponse>> CreateAsync(
        AnnouncementCreateRequest request)
    {
        var title = request.Title.Trim();
        var body = request.Body.Trim();
        var titleZh = string.IsNullOrWhiteSpace(request.TitleZh)
            ? null
            : request.TitleZh.Trim();
        var bodyZh = string.IsNullOrWhiteSpace(request.BodyZh)
            ? null
            : request.BodyZh.Trim();

        if (title.Length < 3 || body.Length < 3)
        {
            return ApiResponse<AnnouncementResponse>.Fail(
                "Announcement title and message are required.", 400);
        }

        var createdAt = DateTimeHelper.MalaysiaNow();
        var notificationId = $"announcement:{Guid.NewGuid():N}";
        var announcement = new Notification
        {
            NotificationId = notificationId,
            UserId = null,
            Title = title,
            Message = body,
            TitleZh = titleZh,
            MessageZh = bodyZh,
            Type = "announcement",
            Actor = "CarePlus Clinic",
            Initials = "CC",
            Href = $"/announcements#{notificationId}",
            Status = "Unread",
            IsPublished = true,
            CreatedAt = createdAt,
        };

        await _announcementRepository.AddAsync(announcement);
        await _announcementRepository.SaveChangesAsync();

        return ApiResponse<AnnouncementResponse>.Ok(new AnnouncementResponse
        {
            Id = notificationId,
            PostedOn = createdAt.ToString("yyyy-MM-dd"),
            Title = title,
            Body = body,
            TitleZh = titleZh ?? title,
            BodyZh = bodyZh ?? body,
        }, "Announcement published.");
    }
}
