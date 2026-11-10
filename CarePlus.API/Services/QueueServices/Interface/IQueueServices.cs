using CarePlus.API.DTO.QueueDTO;

namespace CarePlus.API.Services.QueueServices.Interface
{
    public interface IQueueServices
    {
        Task<ApiResponse<QueueOverviewResponse>> GetOverviewAsync(long userId, string? appointmentId);
        Task<ApiResponse<QueueStatusResponse>> CheckInAsync(long userId, string appointmentId);
        Task<ApiResponse<QueueStatusResponse>> UpdateStatusAsync(long userId, string role, string queueId, string status);
    }
}
