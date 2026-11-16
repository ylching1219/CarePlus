using CarePlus.API.Models.Notification;

namespace CarePlus.API.Repository.Interface;

public interface INotificationRepository
{
    Task AddAsync(Notification notification);
}
