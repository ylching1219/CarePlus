using CarePlus.API.Data;
using CarePlus.API.Models.Room;
using CarePlus.API.Models.RoomAssg;
using CarePlus.API.Models.WorkSch;
using CarePlus.API.Repository.Interface;
using Microsoft.EntityFrameworkCore;

namespace CarePlus.API.Repository;

public class ClinicRoomRepository : IClinicRoomRepository
{
    private readonly AppDbContext _context;

    public ClinicRoomRepository(AppDbContext context)
    {
        _context = context;
    }

    public Task<List<ClinicRoom>> GetAllRoomsAsync()
    {
        return _context.ClinicRooms
            .AsNoTracking()
            .OrderBy(room => room.RoomName)
            .ToListAsync();
    }

    public Task<ClinicRoom?> GetRoomByIdAsync(string roomId)
    {
        return _context.ClinicRooms
            .FirstOrDefaultAsync(room => room.RoomId == roomId);
    }

    public Task<bool> RoomNameExistsAsync(string roomName, string? excludedRoomId = null)
    {
        return _context.ClinicRooms.AnyAsync(room =>
            room.RoomName == roomName
            && (excludedRoomId == null || room.RoomId != excludedRoomId));
    }

    public Task AddRoomAsync(ClinicRoom room)
    {
        return _context.ClinicRooms.AddAsync(room).AsTask();
    }

    public async Task<List<RoomAssignment>> GetRoomAssignmentsAsync(
        DateOnly? date,
        string? scheduleId)
    {
        var query = _context.RoomAssignments
            .AsNoTracking()
            .Include(assignment => assignment.Room)
            .Include(assignment => assignment.Schedule)
                .ThenInclude(schedule => schedule!.Staff)
                    .ThenInclude(staff => staff.User)
            .Include(assignment => assignment.Doctor)
                .ThenInclude(doctor => doctor.User)
            .AsQueryable();

        if (date.HasValue)
        {
            query = query.Where(assignment => assignment.AssignmentDate == date.Value);
        }

        if (!string.IsNullOrWhiteSpace(scheduleId))
        {
            query = query.Where(assignment => assignment.ScheduleId == scheduleId.Trim());
        }

        return await query
            .OrderBy(assignment => assignment.AssignmentDate)
            .ThenBy(assignment => assignment.StartTime)
            .ToListAsync();
    }

    public async Task<List<RoomAssignment>> GetAssignmentsByRoomIdAsync(
        string roomId,
        bool trackChanges = false)
    {
        var query = _context.RoomAssignments
            .Include(assignment => assignment.Room)
            .Where(assignment => assignment.RoomId == roomId)
            .AsQueryable();

        if (!trackChanges)
        {
            query = query.AsNoTracking();
        }

        return await query.ToListAsync();
    }

    public Task<List<RoomAssignment>> GetAssignmentsExceptRoomAsync(string roomId)
    {
        return _context.RoomAssignments
            .AsNoTracking()
            .Where(assignment => assignment.RoomId != roomId)
            .ToListAsync();
    }

    public Task<List<RoomAssignment>> GetAssignmentsForRoomReassignmentAsync(string roomId)
    {
        return _context.RoomAssignments
            .Include(assignment => assignment.Schedule)
                .ThenInclude(schedule => schedule!.Staff)
                    .ThenInclude(staff => staff.User)
            .Where(assignment => assignment.RoomId == roomId
                && assignment.Schedule != null
                && assignment.Schedule.Status == WorkScheduleStatus.Active)
            .OrderBy(assignment => assignment.AssignmentDate)
            .ThenBy(assignment => assignment.StartTime)
            .ToListAsync();
    }

    public Task<RoomAssignment?> GetAssignmentByScheduleIdAsync(
        string scheduleId,
        bool trackChanges = false)
    {
        var query = _context.RoomAssignments
            .Include(assignment => assignment.Room)
            .Where(assignment => assignment.ScheduleId == scheduleId)
            .AsQueryable();

        if (!trackChanges)
        {
            query = query.AsNoTracking();
        }

        return query.FirstOrDefaultAsync();
    }

    public Task<List<RoomAssignment>> GetRoomAssignmentsByScheduleIdsAsync(
        IEnumerable<string> scheduleIds)
    {
        var ids = scheduleIds
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (ids.Count == 0)
        {
            return Task.FromResult(new List<RoomAssignment>());
        }

        return _context.RoomAssignments
            .AsNoTracking()
            .Include(assignment => assignment.Room)
            .Where(assignment => assignment.ScheduleId != null
                && ids.Contains(assignment.ScheduleId))
            .ToListAsync();
    }

    public Task<RoomAssignment?> GetAssignmentByIdAsync(string assignmentId)
    {
        return _context.RoomAssignments
            .FirstOrDefaultAsync(assignment => assignment.AssignmentId == assignmentId);
    }

    public Task<WorkSchedule?> GetScheduleWithStaffAsync(string scheduleId)
    {
        return _context.WorkSchedules
            .Include(schedule => schedule.Staff)
                .ThenInclude(staff => staff.User)
            .FirstOrDefaultAsync(schedule => schedule.ScheduleId == scheduleId);
    }

    public Task<List<ClinicRoom>> GetAvailableRoomsExceptAsync(string roomId)
    {
        return _context.ClinicRooms
            .Where(room => room.RoomId != roomId && room.Status == "Available")
            .OrderBy(room => room.RoomName)
            .ToListAsync();
    }

    public Task AddAssignmentAsync(RoomAssignment assignment)
    {
        return _context.RoomAssignments.AddAsync(assignment).AsTask();
    }

    public void RemoveAssignment(RoomAssignment assignment)
    {
        _context.RoomAssignments.Remove(assignment);
    }

    public Task SaveChangesAsync()
    {
        return _context.SaveChangesAsync();
    }
}
