using CarePlus.API.DTO.WorkScheduleDTO;

namespace CarePlus.API.Services.WorkScheduleServices.Interface
{
    public interface IWorkScheduleServices
    {
        Task<ApiResponse<object>> CreateWorkScheduleAsync(
            WorkScheduleCreateRequest request);
        Task<ApiResponse<object>> RetrieveAllWorkSchedulesAsync(
            long userId,
            string role);
        Task<ApiResponse<object>> RetrieveWorkScheduleByIdAsync(
            string scheduleId,
            long userId,
            string role);
        Task<ApiResponse<object>> RetrieveWorkSchedulesByStaffIdAsync(
            string staffId,
            long userId,
            string role);
        Task<ApiResponse<object>> UpdateWorkScheduleAsync(
            WorkScheduleUpdateRequest request);
    }
}
