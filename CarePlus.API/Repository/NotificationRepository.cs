using CarePlus.API.Data;
using CarePlus.API.Models.Notification;
using CarePlus.API.Repository.Interface;

namespace CarePlus.API.Repository;

public class NotificationRepository : INotificationRepository
{
    private readonly AppDbContext _context;

    public NotificationRepository(AppDbContext context)
    {
        _context = context;
    }

    public Task AddAsync(Notification notification)
    {
        return _context.Notifications.AddAsync(notification).AsTask();
    }
}
