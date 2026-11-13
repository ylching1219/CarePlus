using CarePlus.API.DTO;
using CarePlus.API.DTO.AnnouncementDTO;
using CarePlus.API.DTO.NotificationDTO;

namespace CarePlus.API.Services.NotificationServices.Interface;

public interface INotificationServices
{
    Task<ApiResponse<List<AnnouncementResponse>>> ListPublishedAsync();
    Task<ApiResponse<List<NotificationResponse>>> ListAsync(string patientId);
    Task<ApiResponse<List<NotificationResponse>>> MarkReadAsync(string patientId, string notificationId);
    Task<ApiResponse<List<NotificationResponse>>> MarkManyReadAsync(string patientId, List<string> notificationIds);
    Task<ApiResponse<List<NotificationResponse>>> MarkAllReadAsync(string patientId);
    Task<ApiResponse<List<NotificationResponse>>> DeleteManyAsync(string patientId, List<string> notificationIds);
    Task<ApiResponse<List<NotificationResponse>>> ListStaffAsync(long userId);
    Task<ApiResponse<List<NotificationResponse>>> MarkStaffReadAsync(long userId, string notificationId);
    Task<ApiResponse<List<NotificationResponse>>> MarkManyStaffReadAsync(long userId, List<string> notificationIds);
    Task<ApiResponse<List<NotificationResponse>>> MarkAllStaffReadAsync(long userId);
    Task<ApiResponse<List<NotificationResponse>>> DeleteManyStaffAsync(long userId, List<string> notificationIds);
}
