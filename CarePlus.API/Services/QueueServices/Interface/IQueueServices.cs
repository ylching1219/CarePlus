using CarePlus.API.DTO.QueueDTO;

namespace CarePlus.API.Services.QueueServices.Interface
{
    public interface IQueueServices
    {
        Task<ApiResponse<QueueOverviewResponse>> GetOverviewAsync(long userId, string? appointmentId);
        Task<ApiResponse<QueueStatusResponse>> CheckInAsync(long userId, string appointmentId);
        Task<ApiResponse<List<StaffQueueEntryResponse>>> GetStaffQueueAsync(long userId, string role, DateOnly? date);
        Task<ApiResponse<QueueStatusResponse>> UpdateStatusAsync(long userId, string role, string queueId, string status, string? expectedStatus = null);
        Task<ApiResponse<QueueStatusResponse>> CompleteConsultationAsync(long userId, string queueId, QueueConsultationRequest request);
    }
}
