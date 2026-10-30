using CarePlus.API.DTO;
using CarePlus.API.DTO.NotificationDTO;

namespace CarePlus.API.Services.NotificationServices.Interface;

public interface INotificationServices
{
    Task<ApiResponse<List<NotificationResponse>>> ListAsync(string patientId);
    Task<ApiResponse<List<NotificationResponse>>> MarkReadAsync(string patientId, string notificationId);
    Task<ApiResponse<List<NotificationResponse>>> MarkManyReadAsync(string patientId, List<string> notificationIds);
    Task<ApiResponse<List<NotificationResponse>>> MarkAllReadAsync(string patientId);
    Task<ApiResponse<List<NotificationResponse>>> DeleteManyAsync(string patientId, List<string> notificationIds);
}
