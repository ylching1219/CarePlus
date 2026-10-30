using CarePlus.API.DTO.AppointmentDTO;

namespace CarePlus.API.Services.AppointmentServices.Interface
{
    public interface IAppointmentServices
    {
        Task<ApiResponse<BookingCatalogResponse>> GetCatalogAsync(long userId);
        Task<ApiResponse<BookingSlotsResponse>> GetSlotsAsync(long userId, string date, string doctorId);
        Task<ApiResponse<List<AppointmentResponse>>> ListUpcomingAsync(long userId);
        Task<ApiResponse<List<AppointmentResponse>>> ListHistoryAsync(long userId);
        Task<ApiResponse<AppointmentResponse>> BookAsync(long userId, AppointmentBookRequest request);
        Task<ApiResponse<AppointmentResponse>> CancelAsync(long userId, string appointmentId);
        Task<ApiResponse<AppointmentResponse>> RescheduleAsync(long userId, string appointmentId, AppointmentRescheduleRequest request);
    }
}
