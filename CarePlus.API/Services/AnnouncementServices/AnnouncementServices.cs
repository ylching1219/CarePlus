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
        var rows = await _db.ClinicAnnouncements
            .AsNoTracking()
            .Where(row => row.IsPublished && row.PublishedAt <= DateTimeHelper.MalaysiaNow())
            .OrderByDescending(row => row.PublishedAt)
            .ToListAsync();

        return ApiResponse<List<AnnouncementResponse>>.Ok(rows.Select(row => new AnnouncementResponse
        {
            Id = row.AnnouncementId,
            PostedOn = row.PublishedAt.ToString("yyyy-MM-dd"),
            Title = row.Title,
            Body = row.Body,
            TitleZh = row.TitleZh,
            BodyZh = row.BodyZh,
        }).ToList());
    }
}
