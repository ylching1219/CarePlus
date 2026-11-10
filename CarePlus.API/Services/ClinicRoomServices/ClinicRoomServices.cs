using CarePlus.API.Data;
using CarePlus.API.DTO.ClinicRoomDTO;
using CarePlus.API.Models.Room;
using CarePlus.API.Models.RoomAssg;
using CarePlus.API.Services.ClinicRoomServices.Interface;
using CarePlus.API.Services.CounterServices.Interface;
using Microsoft.EntityFrameworkCore;

namespace CarePlus.API.Services.ClinicRoomServices;

public class ClinicRoomServices : IClinicRoomServices
{
    private static readonly HashSet<string> ValidStatuses = new(StringComparer.OrdinalIgnoreCase)
    {
        "Available",
        "Occupied",
        "Maintenance",
        "Inactive",
    };

    private readonly AppDbContext _db;
    private readonly ICounterServices _counterService;

    public ClinicRoomServices(AppDbContext db, ICounterServices counterService)
    {
        _db = db;
        _counterService = counterService;
    }

    public async Task<ApiResponse<List<ClinicRoomResponse>>> RetrieveAllAsync()
    {
        var rooms = await _db.ClinicRooms
            .AsNoTracking()
            .OrderBy(room => room.RoomName)
            .Select(room => ToResponse(room))
            .ToListAsync();

        return ApiResponse<List<ClinicRoomResponse>>.Ok(rooms);
    }

    public async Task<ApiResponse<ClinicRoomResponse>> CreateAsync(ClinicRoomCreateRequest request)
    {
        var roomName = request.RoomName.Trim();
        if (roomName.Length < 2)
        {
            return ApiResponse<ClinicRoomResponse>.Fail("Room name is required.", 400);
        }

        var status = NormalizeStatus(request.Status);
        if (status == null)
        {
            return ApiResponse<ClinicRoomResponse>.Fail(
                "Status must be Available, Occupied, Maintenance, or Inactive.", 400);
        }

        if (await _db.ClinicRooms.AnyAsync(room => room.RoomName == roomName))
        {
            return ApiResponse<ClinicRoomResponse>.Fail("A room with this name already exists.", 409);
        }

        var room = new ClinicRoom
        {
            RoomId = await _counterService.GenerateNextIdAsync("RoomId"),
            RoomName = roomName,
            Status = status,
        };

        await _db.ClinicRooms.AddAsync(room);
        await _db.SaveChangesAsync();

        return ApiResponse<ClinicRoomResponse>.Ok(ToResponse(room));
    }

    public async Task<ApiResponse<ClinicRoomResponse>> UpdateAsync(ClinicRoomUpdateRequest request)
    {
        var room = await _db.ClinicRooms
            .FirstOrDefaultAsync(item => item.RoomId == request.RoomId.Trim());

        if (room == null)
        {
            return ApiResponse<ClinicRoomResponse>.Fail("Clinic room not found.", 404);
        }

        var roomName = request.RoomName.Trim();
        var status = NormalizeStatus(request.Status);
        if (roomName.Length < 2)
        {
            return ApiResponse<ClinicRoomResponse>.Fail("Room name is required.", 400);
        }

        if (status == null)
        {
            return ApiResponse<ClinicRoomResponse>.Fail(
                "Status must be Available, Occupied, Maintenance, or Inactive.", 400);
        }

        if (await _db.ClinicRooms.AnyAsync(item =>
                item.RoomId != room.RoomId && item.RoomName == roomName))
        {
            return ApiResponse<ClinicRoomResponse>.Fail("A room with this name already exists.", 409);
        }

        room.RoomName = roomName;
        room.Status = status;
        await _db.SaveChangesAsync();

        return ApiResponse<ClinicRoomResponse>.Ok(ToResponse(room));
    }

    public async Task<ApiResponse<List<RoomAssignmentResponse>>> RetrieveAssignmentsAsync(
        DateOnly? date,
        string? scheduleId)
    {
        var query = _db.RoomAssignments
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

        var assignments = await query
            .OrderBy(assignment => assignment.AssignmentDate)
            .ThenBy(assignment => assignment.StartTime)
            .ToListAsync();

        return ApiResponse<List<RoomAssignmentResponse>>.Ok(
            assignments.Select(ToAssignmentResponse).ToList());
    }

    public async Task<ApiResponse<RoomAssignmentResponse>> AssignAsync(
        RoomAssignmentCreateRequest request)
    {
        var scheduleId = request.ScheduleId.Trim();
        var roomId = request.RoomId.Trim();

        var schedule = await _db.WorkSchedules
            .Include(item => item.Staff)
                .ThenInclude(staff => staff.User)
            .FirstOrDefaultAsync(item => item.ScheduleId == scheduleId);

        if (schedule == null)
        {
            return ApiResponse<RoomAssignmentResponse>.Fail("Work schedule not found.", 404);
        }

        if (!string.Equals(schedule.Staff.Role, "Doctor", StringComparison.OrdinalIgnoreCase))
        {
            return ApiResponse<RoomAssignmentResponse>.Fail(
                "Only doctor schedules can be assigned to clinic rooms.", 400);
        }

        if (!string.Equals(schedule.Status, "Active", StringComparison.OrdinalIgnoreCase))
        {
            return ApiResponse<RoomAssignmentResponse>.Fail(
                "Only active schedules can be assigned to a clinic room.", 400);
        }

        var assignment = await _db.RoomAssignments
            .FirstOrDefaultAsync(item => item.ScheduleId == scheduleId);

        var room = await _db.ClinicRooms
            .FirstOrDefaultAsync(item => item.RoomId == roomId);
        if (room == null)
        {
            return ApiResponse<RoomAssignmentResponse>.Fail("Clinic room not found.", 404);
        }

        if (!string.Equals(room.Status, "Available", StringComparison.OrdinalIgnoreCase))
        {
            return ApiResponse<RoomAssignmentResponse>.Fail(
                "Only available clinic rooms can be assigned.", 409);
        }

        var (startTime, endTime) = ShiftBounds(schedule.Shift);
        var requestedStart = schedule.WorkingDate.ToDateTime(startTime);
        var requestedEnd = schedule.WorkingDate.ToDateTime(endTime);
        if (requestedEnd <= requestedStart)
        {
            requestedEnd = requestedEnd.AddDays(1);
        }

        var roomAssignments = await _db.RoomAssignments
            .AsNoTracking()
            .Where(item => item.RoomId == roomId)
            .ToListAsync();

        var roomConflict = roomAssignments.Any(item =>
        {
            var existingStart = item.AssignmentDate.ToDateTime(item.StartTime);
            var existingEnd = item.AssignmentDate.ToDateTime(item.EndTime);
            if (existingEnd <= existingStart)
            {
                existingEnd = existingEnd.AddDays(1);
            }

            return item.AssignmentId != assignment?.AssignmentId
                && requestedStart < existingEnd
                && existingStart < requestedEnd;
        });

        if (roomConflict)
        {
            return ApiResponse<RoomAssignmentResponse>.Fail(
                "This clinic room is already assigned during that shift.", 409);
        }

        var doctor = await _db.Doctors
            .Include(item => item.User)
            .FirstOrDefaultAsync(item => item.StaffId == schedule.StaffId);
        if (doctor == null)
        {
            return ApiResponse<RoomAssignmentResponse>.Fail("Doctor profile not found.", 404);
        }

        assignment ??= new RoomAssignment
        {
            AssignmentId = $"RA-{Guid.NewGuid():N}"[..11].ToUpperInvariant(),
            ScheduleId = schedule.ScheduleId,
        };

        assignment.RoomId = room.RoomId;
        assignment.DoctorId = schedule.StaffId;
        assignment.AssignmentDate = schedule.WorkingDate;
        assignment.StartTime = startTime;
        assignment.EndTime = endTime;
        assignment.Room = room;
        assignment.Schedule = schedule;
        assignment.Doctor = doctor;

        if (_db.Entry(assignment).State == EntityState.Detached)
        {
            _db.RoomAssignments.Add(assignment);
        }
        await _db.SaveChangesAsync();

        return ApiResponse<RoomAssignmentResponse>.Ok(ToAssignmentResponse(assignment));
    }

    public async Task<ApiResponse<object>> RemoveAssignmentAsync(string assignmentId)
    {
        var assignment = await _db.RoomAssignments
            .FirstOrDefaultAsync(item => item.AssignmentId == assignmentId.Trim());
        if (assignment == null)
        {
            return ApiResponse<object>.Fail("Room assignment not found.", 404);
        }

        _db.RoomAssignments.Remove(assignment);
        await _db.SaveChangesAsync();
        return ApiResponse<object>.Ok(
            new { assignmentId = assignment.AssignmentId },
            "Room assignment removed.");
    }

    private static RoomAssignmentResponse ToAssignmentResponse(RoomAssignment assignment) => new()
    {
        AssignmentId = assignment.AssignmentId,
        ScheduleId = assignment.ScheduleId ?? string.Empty,
        RoomId = assignment.RoomId,
        RoomName = assignment.Room?.RoomName ?? string.Empty,
        DoctorId = assignment.DoctorId,
        DoctorName = assignment.Doctor?.User?.Name
            ?? assignment.Schedule?.Staff?.User?.Name
            ?? assignment.DoctorId,
        AssignmentDate = assignment.AssignmentDate,
        Shift = assignment.Schedule?.Shift ?? string.Empty,
        StartTime = assignment.StartTime,
        EndTime = assignment.EndTime,
    };

    private static (TimeOnly Start, TimeOnly End) ShiftBounds(string shift)
    {
        return string.Equals(shift, "Night", StringComparison.OrdinalIgnoreCase)
            ? (new TimeOnly(18, 0), new TimeOnly(6, 0))
            : (new TimeOnly(6, 0), new TimeOnly(18, 0));
    }

    private static ClinicRoomResponse ToResponse(ClinicRoom room) => new()
    {
        RoomId = room.RoomId,
        RoomName = room.RoomName,
        Status = room.Status,
    };

    private static string? NormalizeStatus(string? status)
    {
        var value = string.IsNullOrWhiteSpace(status) ? "Available" : status.Trim();
        return ValidStatuses.FirstOrDefault(item =>
            item.Equals(value, StringComparison.OrdinalIgnoreCase));
    }
}
