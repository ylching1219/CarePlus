using CarePlus.API.Models.WorkSch;

namespace CarePlus.API.Repository.Interface
{
    public interface IWorkScheduleRepository
    {
        Task<string> CreateWorkScheduleAsync(WorkSchedule workSchedule);
        Task<WorkSchedule?> GetWorkScheduleByIdAsync(string scheduleId);
        Task<List<WorkSchedule>> GetAllWorkSchedulesAsync();
        Task<List<WorkSchedule>> GetWorkSchedulesByStaffIdAsync(string staffId);
        Task<bool> HasActiveScheduleOnDateAsync(
            string staffId,
            DateOnly workingDate,
            string? excludedScheduleId = null);
        Task<bool> UpdateWorkScheduleAsync(WorkSchedule workSchedule);
    }
}
