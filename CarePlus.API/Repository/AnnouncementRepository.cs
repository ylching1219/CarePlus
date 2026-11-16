using CarePlus.API.Data;
using CarePlus.API.Models.Notification;
using CarePlus.API.Repository.Interface;

namespace CarePlus.API.Repository;

public class AnnouncementRepository : IAnnouncementRepository
{
    private readonly AppDbContext _context;

    public AnnouncementRepository(AppDbContext context)
    {
        _context = context;
    }

    public Task AddAsync(Notification announcement)
    {
        return _context.Notifications.AddAsync(announcement).AsTask();
    }

    public Task SaveChangesAsync()
    {
        return _context.SaveChangesAsync();
    }
}
