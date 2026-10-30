using CarePlus.API.Models.Leave;

namespace CarePlus.API.Repository.Interface
{
    public interface ILeaveRepository
    {
        Task<string> CreateLeaveAsync(Leave leave);
        Task<Leave?> GetLeaveByIdAsync(string leaveId);
        Task<List<Leave>> GetAllLeavesAsync();
        Task<List<Leave>> GetLeavesByStaffIdAsync(string staffId);
        Task<bool> HasOverlappingLeaveAsync(
            string staffId,
            DateTime startDate,
            DateTime endDate,
            string? excludedLeaveId = null);
        Task<bool> UpdateLeaveAsync(Leave leave);
    }
}
