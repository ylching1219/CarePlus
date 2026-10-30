using CarePlus.API.Data;
using CarePlus.API.Models.Leave;
using CarePlus.API.Repository.Interface;
using Microsoft.EntityFrameworkCore;

namespace CarePlus.API.Repository
{
    public class LeaveRepository : ILeaveRepository
    {
        private readonly AppDbContext _context;

        public LeaveRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<string> CreateLeaveAsync(Leave leave)
        {
            await _context.LeaveApplications.AddAsync(leave);
            await _context.SaveChangesAsync();

            return leave.LeaveId;
        }

        public Task<Leave?> GetLeaveByIdAsync(string leaveId)
        {
            return _context.LeaveApplications
                .Include(leave => leave.Staff)
                .ThenInclude(staff => staff.User)
                .FirstOrDefaultAsync(leave => leave.LeaveId == leaveId);
        }

        public Task<List<Leave>> GetAllLeavesAsync()
        {
            return _context.LeaveApplications
                .Include(leave => leave.Staff)
                .ThenInclude(staff => staff.User)
                .OrderBy(leave => leave.StartDate)
                .ThenBy(leave => leave.LeaveId)
                .ToListAsync();
        }

        public Task<List<Leave>> GetLeavesByStaffIdAsync(string staffId)
        {
            return _context.LeaveApplications
                .Include(leave => leave.Staff)
                .ThenInclude(staff => staff.User)
                .Where(leave => leave.StaffId == staffId)
                .OrderByDescending(leave => leave.StartDate)
                .ThenBy(leave => leave.LeaveId)
                .ToListAsync();
        }

        public Task<bool> HasOverlappingLeaveAsync(
            string staffId,
            DateTime startDate,
            DateTime endDate,
            string? excludedLeaveId = null)
        {
            return _context.LeaveApplications.AnyAsync(leave =>
                leave.StaffId == staffId
                && leave.Status != "Declined"
                && (excludedLeaveId == null || leave.LeaveId != excludedLeaveId)
                && startDate <= leave.EndDate
                && endDate >= leave.StartDate);
        }

        public async Task<bool> UpdateLeaveAsync(Leave leave)
        {
            _context.LeaveApplications.Update(leave);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
