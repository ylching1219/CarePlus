using CarePlus.API.Data;
using CarePlus.API.Models.WorkSch;
using CarePlus.API.Repository.Interface;
using Microsoft.EntityFrameworkCore;

namespace CarePlus.API.Repository
{
    public class WorkScheduleRepository : IWorkScheduleRepository
    {
        private readonly AppDbContext _context;

        public WorkScheduleRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<string> CreateWorkScheduleAsync(WorkSchedule workSchedule)
        {
            await _context.WorkSchedules.AddAsync(workSchedule);
            await _context.SaveChangesAsync();

            return workSchedule.ScheduleId;
        }

        public async Task<WorkSchedule?> GetWorkScheduleByIdAsync(string scheduleId)
        {
            return await _context.WorkSchedules
                .Include(schedule => schedule.Staff)
                .ThenInclude(staff => staff.User)
                .FirstOrDefaultAsync(schedule => schedule.ScheduleId == scheduleId);
        }

        public async Task<List<WorkSchedule>> GetAllWorkSchedulesAsync()
        {
            return await _context.WorkSchedules
                .Include(schedule => schedule.Staff)
                .ThenInclude(staff => staff.User)
                .Where(schedule => schedule.Staff.Role == "Doctor"
                    || schedule.Staff.Role == "Pharmacist")
                .OrderBy(schedule => schedule.WorkingDate)
                .ThenBy(schedule => schedule.Shift)
                .ToListAsync();
        }

        public async Task<List<WorkSchedule>> GetWorkSchedulesByStaffIdAsync(string staffId)
        {
            return await _context.WorkSchedules
                .Include(schedule => schedule.Staff)
                .ThenInclude(staff => staff.User)
                .Where(schedule => schedule.StaffId == staffId
                    && (schedule.Staff.Role == "Doctor"
                        || schedule.Staff.Role == "Pharmacist"))
                .OrderBy(schedule => schedule.WorkingDate)
                .ThenBy(schedule => schedule.Shift)
                .ToListAsync();
        }

        public Task<bool> HasActiveScheduleOnDateAsync(
            string staffId,
            DateOnly workingDate,
            string? excludedScheduleId = null)
        {
            return _context.WorkSchedules.AnyAsync(schedule =>
                schedule.StaffId == staffId &&
                schedule.WorkingDate == workingDate &&
                schedule.Status == "Active" &&
                (excludedScheduleId == null || schedule.ScheduleId != excludedScheduleId));
        }

        public async Task<bool> UpdateWorkScheduleAsync(WorkSchedule workSchedule)
        {
            _context.WorkSchedules.Update(workSchedule);
            await _context.SaveChangesAsync();

            return true;
        }
    }
}
