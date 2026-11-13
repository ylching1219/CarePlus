using CarePlus.API.DTO.LeaveDTO;

namespace CarePlus.API.Services.LeaveServices.Interface
{
    public interface ILeaveServices
    {
        Task<ApiResponse<object>> CreateLeaveAsync(LeaveCreateRequest request);
        Task<ApiResponse<object>> RetrieveAllLeavesAsync();
        Task<ApiResponse<object>> RetrieveLeaveByIdAsync(string leaveId);
        Task<ApiResponse<object>> RetrieveLeavesByStaffIdAsync(string staffId);
        Task<ApiResponse<object>> UpdateLeaveStatusAsync(
            LeaveStatusUpdateRequest request);
    }
}
