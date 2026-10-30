using CarePlus.API.DTO.WorkScheduleDTO;

namespace CarePlus.API.Services.WorkScheduleServices.Interface
{
    public interface IWorkScheduleServices
    {
        Task<ApiResponse<object>> CreateWorkScheduleAsync(
            WorkScheduleCreateRequest request);
        Task<ApiResponse<object>> RetrieveAllWorkSchedulesAsync();
        Task<ApiResponse<object>> RetrieveWorkScheduleByIdAsync(string scheduleId);
        Task<ApiResponse<object>> RetrieveWorkSchedulesByStaffIdAsync(string staffId);
        Task<ApiResponse<object>> UpdateWorkScheduleAsync(
            WorkScheduleUpdateRequest request);
    }
}
