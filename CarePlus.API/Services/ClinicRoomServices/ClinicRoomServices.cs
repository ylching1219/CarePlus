using CarePlus.API.DTO.ClinicRoomDTO;
using CarePlus.API.Extension;
using CarePlus.API.Models.Room;
using CarePlus.API.Models.RoomAssg;
using CarePlus.API.Models.Notification;
using CarePlus.API.Models.WorkSch;
using CarePlus.API.Repository.Interface;
using CarePlus.API.Services.ClinicRoomServices.Interface;
using CarePlus.API.Services.CounterServices.Interface;

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

    private readonly IClinicRoomRepository _roomRepo;
    private readonly INotificationRepository _notificationRepo;
    private readonly IStaffRepository _staffRepo;
    private readonly IDoctorRepository _doctorRepo;
    private readonly ICounterServices _counterService;

    public ClinicRoomServices(
        IClinicRoomRepository roomRepo,
        INotificationRepository notificationRepo,
        IStaffRepository staffRepo,
        IDoctorRepository doctorRepo,
        ICounterServices counterService)
    {
        _roomRepo = roomRepo;
        _notificationRepo = notificationRepo;
        _staffRepo = staffRepo;
        _doctorRepo = doctorRepo;
        _counterService = counterService;
    }

    public async Task<ApiResponse<List<ClinicRoomResponse>>> RetrieveAllAsync()
    {
        var rooms = await _roomRepo.GetAllRoomsAsync();

        return ApiResponse<List<ClinicRoomResponse>>.Ok(
            rooms.Select(ToResponse).ToList());
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

        if (await _roomRepo.RoomNameExistsAsync(roomName))
        {
            return ApiResponse<ClinicRoomResponse>.Fail("A room with this name already exists.", 409);
        }

        var room = new ClinicRoom
        {
            RoomId = await _counterService.GenerateNextIdAsync("RoomId"),
            RoomName = roomName,
            Status = status,
        };

        await _roomRepo.AddRoomAsync(room);
        await _roomRepo.SaveChangesAsync();

        return ApiResponse<ClinicRoomResponse>.Ok(ToResponse(room));
    }

    public async Task<ApiResponse<ClinicRoomResponse>> UpdateAsync(ClinicRoomUpdateRequest request)
    {
        var room = await _roomRepo.GetRoomByIdAsync(request.RoomId.Trim());

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

        if (await _roomRepo.RoomNameExistsAsync(roomName, room.RoomId))
        {
            return ApiResponse<ClinicRoomResponse>.Fail("A room with this name already exists.", 409);
        }

        var becameUnavailable = !string.Equals(room.Status, status, StringComparison.OrdinalIgnoreCase)
            && !string.Equals(status, "Available", StringComparison.OrdinalIgnoreCase);
        room.RoomName = roomName;
        room.Status = status;

        if (becameUnavailable)
        {
            await ReassignAffectedSchedulesAsync(room);
        }

        await _roomRepo.SaveChangesAsync();

        return ApiResponse<ClinicRoomResponse>.Ok(ToResponse(room));
    }

    public async Task<ApiResponse<List<RoomAssignmentResponse>>> RetrieveAssignmentsAsync(
        DateOnly? date,
        string? scheduleId)
    {
        var assignments = await _roomRepo.GetRoomAssignmentsAsync(date, scheduleId);

        return ApiResponse<List<RoomAssignmentResponse>>.Ok(
            assignments.Select(ToAssignmentResponse).ToList());
    }

    public async Task<ApiResponse<RoomAssignmentResponse>> AssignAsync(
        RoomAssignmentCreateRequest request)
    {
        var scheduleId = request.ScheduleId.Trim();
        var roomId = request.RoomId.Trim();

        var schedule = await _roomRepo.GetScheduleWithStaffAsync(scheduleId);

        if (schedule == null)
        {
            return ApiResponse<RoomAssignmentResponse>.Fail("Work schedule not found.", 404);
        }

        if (!string.Equals(schedule.Staff.Role, "Doctor", StringComparison.OrdinalIgnoreCase))
        {
            return ApiResponse<RoomAssignmentResponse>.Fail(
                "Only doctor schedules can be assigned to clinic rooms.", 400);
        }

        if (!string.Equals(schedule.Status, WorkScheduleStatus.Active, StringComparison.OrdinalIgnoreCase)
            && !string.Equals(schedule.Status, WorkScheduleStatus.RoomReassignmentRequired, StringComparison.OrdinalIgnoreCase))
        {
            return ApiResponse<RoomAssignmentResponse>.Fail(
                "Only active schedules or schedules requiring room reassignment can be assigned to a clinic room.", 400);
        }

        var assignment = await _roomRepo.GetAssignmentByScheduleIdAsync(
            scheduleId,
            trackChanges: true);

        var room = await _roomRepo.GetRoomByIdAsync(roomId);
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

        var roomAssignments = await _roomRepo.GetAssignmentsByRoomIdAsync(roomId);

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

        var doctor = await _doctorRepo.GetDoctorByIdAsync(schedule.StaffId);
        if (doctor == null)
        {
            return ApiResponse<RoomAssignmentResponse>.Fail("Doctor profile not found.", 404);
        }

        if (assignment == null)
        {
            assignment = new RoomAssignment
            {
                AssignmentId = $"RA-{Guid.NewGuid():N}"[..11].ToUpperInvariant(),
                ScheduleId = schedule.ScheduleId,
            };
            await _roomRepo.AddAssignmentAsync(assignment);
        }

        assignment.RoomId = room.RoomId;
        assignment.DoctorId = schedule.StaffId;
        assignment.AssignmentDate = schedule.WorkingDate;
        assignment.StartTime = startTime;
        assignment.EndTime = endTime;
        assignment.Room = room;
        assignment.Schedule = schedule;
        assignment.Doctor = doctor;
        schedule.Status = WorkScheduleStatus.Active;
        await _roomRepo.SaveChangesAsync();

        return ApiResponse<RoomAssignmentResponse>.Ok(ToAssignmentResponse(assignment));
    }

    public async Task<ApiResponse<object>> RemoveAssignmentAsync(string assignmentId)
    {
        var assignment = await _roomRepo.GetAssignmentByIdAsync(assignmentId.Trim());
        if (assignment == null)
        {
            return ApiResponse<object>.Fail("Room assignment not found.", 404);
        }

        _roomRepo.RemoveAssignment(assignment);
        await _roomRepo.SaveChangesAsync();
        return ApiResponse<object>.Ok(
            new { assignmentId = assignment.AssignmentId },
            "Room assignment removed.");
    }

    private async Task ReassignAffectedSchedulesAsync(ClinicRoom unavailableRoom)
    {
        var affectedAssignments = await _roomRepo
            .GetAssignmentsForRoomReassignmentAsync(unavailableRoom.RoomId);

        if (affectedAssignments.Count == 0)
        {
            return;
        }

        var availableRooms = await _roomRepo
            .GetAvailableRoomsExceptAsync(unavailableRoom.RoomId);
        var existingAssignments = await _roomRepo
            .GetAssignmentsExceptRoomAsync(unavailableRoom.RoomId);
        var reservedRooms = new List<RoomReservation>();
        var adminStaff = await _staffRepo.GetAllStaffsByAsync(
            staff => staff.Role == "Admin");
        var adminUserIds = adminStaff
            .Select(staff => staff.UserId)
            .ToList();
        var now = DateTimeHelper.MalaysiaNow();

        foreach (var assignment in affectedAssignments)
        {
            var schedule = assignment.Schedule;
            if (schedule == null)
            {
                continue;
            }

            var assignedStaff = schedule.Staff;
            if (assignedStaff == null)
            {
                continue;
            }

            var requestedStart = assignment.AssignmentDate.ToDateTime(assignment.StartTime);
            var requestedEnd = assignment.AssignmentDate.ToDateTime(assignment.EndTime);
            if (requestedEnd <= requestedStart)
            {
                requestedEnd = requestedEnd.AddDays(1);
            }

            if (requestedEnd <= now)
            {
                continue;
            }

            var replacement = availableRooms.FirstOrDefault(room =>
                !existingAssignments.Any(existing =>
                    existing.RoomId == room.RoomId
                    && existing.AssignmentId != assignment.AssignmentId
                    && Overlaps(requestedStart, requestedEnd, existing.AssignmentDate, existing.StartTime, existing.EndTime))
                && !reservedRooms.Any(reserved =>
                    reserved.RoomId == room.RoomId
                    && Overlaps(requestedStart, requestedEnd, reserved.AssignmentDate, reserved.StartTime, reserved.EndTime)));

            var doctorName = schedule.Staff?.User?.Name ?? schedule.StaffId;
            if (replacement != null)
            {
                assignment.RoomId = replacement.RoomId;
                assignment.Room = replacement;
                reservedRooms.Add(new RoomReservation(
                    replacement.RoomId,
                    assignment.AssignmentDate,
                    assignment.StartTime,
                    assignment.EndTime));

                var message = $"Your {schedule.Shift.ToLowerInvariant()} schedule on {schedule.WorkingDate:dd MMM yyyy} was moved from {unavailableRoom.RoomName} to {replacement.RoomName} because the original room is {unavailableRoom.Status.ToLowerInvariant()}.";
                await QueueRoomNotificationAsync(
                    assignedStaff.UserId,
                    schedule.ScheduleId,
                    "Consultation room reassigned",
                    message);
                await QueueAdminNotificationsAsync(
                    adminUserIds,
                    schedule.ScheduleId,
                    "Consultation room reassigned",
                    $"{doctorName}'s {schedule.Shift.ToLowerInvariant()} schedule on {schedule.WorkingDate:dd MMM yyyy} was moved from {unavailableRoom.RoomName} to {replacement.RoomName}.");
            }
            else
            {
                _roomRepo.RemoveAssignment(assignment);
                schedule.Status = WorkScheduleStatus.RoomReassignmentRequired;

                await QueueRoomNotificationAsync(
                    assignedStaff.UserId,
                    schedule.ScheduleId,
                    "Room reassignment required",
                    $"Your {schedule.Shift.ToLowerInvariant()} schedule on {schedule.WorkingDate:dd MMM yyyy} no longer has an available consultation room. An administrator must assign a new room before appointments can be booked.");
                await QueueAdminNotificationsAsync(
                    adminUserIds,
                    schedule.ScheduleId,
                    "Room reassignment required",
                    $"{doctorName}'s {schedule.Shift.ToLowerInvariant()} schedule on {schedule.WorkingDate:dd MMM yyyy} needs a new room because {unavailableRoom.RoomName} is {unavailableRoom.Status.ToLowerInvariant()}.");
            }
        }
    }

    private async Task QueueAdminNotificationsAsync(
        IEnumerable<long> adminUserIds,
        string scheduleId,
        string title,
        string message)
    {
        foreach (var adminUserId in adminUserIds)
        {
            await QueueRoomNotificationAsync(adminUserId, scheduleId, title, message);
        }
    }

    private Task QueueRoomNotificationAsync(
        long userId,
        string scheduleId,
        string title,
        string message)
    {
        return _notificationRepo.AddAsync(new Notification
        {
            NotificationId = $"room-assignment:{scheduleId}:{Guid.NewGuid():N}:user:{userId}",
            UserId = userId,
            Title = title,
            Message = message,
            Type = "room_assignment",
            Actor = "CarePlus Clinic",
            Initials = "CC",
            Href = "/staff/schedules",
            Status = "Unread",
            IsPublished = true,
            CreatedAt = DateTimeHelper.MalaysiaNow(),
        });
    }

    private static bool Overlaps(
        DateTime requestedStart,
        DateTime requestedEnd,
        DateOnly existingDate,
        TimeOnly existingStartTime,
        TimeOnly existingEndTime)
    {
        var existingStart = existingDate.ToDateTime(existingStartTime);
        var existingEnd = existingDate.ToDateTime(existingEndTime);
        if (existingEnd <= existingStart)
        {
            existingEnd = existingEnd.AddDays(1);
        }

        return requestedStart < existingEnd && existingStart < requestedEnd;
    }

    private sealed record RoomReservation(
        string RoomId,
        DateOnly AssignmentDate,
        TimeOnly StartTime,
        TimeOnly EndTime);

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
