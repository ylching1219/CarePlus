using CarePlus.API.Data;
using CarePlus.API.DTO;
using CarePlus.API.DTO.AnnouncementDTO;
using CarePlus.API.Extension;
using CarePlus.API.Services.AnnouncementServices.Interface;
using Microsoft.EntityFrameworkCore;

namespace CarePlus.API.Services.AnnouncementServices;

public class AnnouncementServices : IAnnouncementServices
{
    private readonly AppDbContext _db;

    public AnnouncementServices(AppDbContext db)
    {
        _db = db;
    }

    public async Task<ApiResponse<List<AnnouncementResponse>>> ListPublishedAsync()
    {
        var rows = await _db.Notifications
            .AsNoTracking()
            .Where(row => row.UserId == null
                && row.Type == "announcement"
                && row.IsPublished
                && row.Status != "Deleted"
                && row.CreatedAt <= DateTimeHelper.MalaysiaNow())
            .OrderByDescending(row => row.CreatedAt)
            .ToListAsync();

        return ApiResponse<List<AnnouncementResponse>>.Ok(rows.Select(row => new AnnouncementResponse
        {
            Id = row.NotificationId,
            PostedOn = row.CreatedAt.ToString("yyyy-MM-dd"),
            Title = row.Title,
            Body = row.Message,
            TitleZh = row.TitleZh ?? row.Title,
            BodyZh = row.MessageZh ?? row.Message,
        }).ToList());
    }
}
