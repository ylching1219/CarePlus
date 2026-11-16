using CarePlus.API.Models.Notification;

namespace CarePlus.API.Repository.Interface;

public interface IAnnouncementRepository
{
    Task AddAsync(Notification announcement);
    Task SaveChangesAsync();
}
