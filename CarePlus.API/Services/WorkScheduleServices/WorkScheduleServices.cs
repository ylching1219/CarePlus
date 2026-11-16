using CarePlus.API.DTO.WorkScheduleDTO;
using CarePlus.API.Models.Room;
using CarePlus.API.Models.RoomAssg;
using CarePlus.API.Models.WorkSch;
using CarePlus.API.Repository.Interface;
using CarePlus.API.Services.CounterServices.Interface;
using CarePlus.API.Services.WorkScheduleServices.Interface;

namespace CarePlus.API.Services.WorkScheduleServices
{
    public class WorkScheduleServices : IWorkScheduleServices
    {
        private static readonly HashSet<string> ValidStatuses =
            new(StringComparer.Ordinal)
            {
                WorkScheduleStatus.Active,
                WorkScheduleStatus.Inactive,
                WorkScheduleStatus.RoomReassignmentRequired
            };

        private static readonly HashSet<string> ValidShifts =
            new(StringComparer.OrdinalIgnoreCase)
            {
                "Morning",
                "Night"
            };

        private readonly IWorkScheduleRepository _workScheduleRepo;
        private readonly IClinicRoomRepository _roomRepo;
        private readonly IStaffRepository _staffRepo;
        private readonly ICounterServices _counterService;

        public WorkScheduleServices(
            IWorkScheduleRepository workScheduleRepo,
            IClinicRoomRepository roomRepo,
            IStaffRepository staffRepo,
            ICounterServices counterService)
        {
            _workScheduleRepo = workScheduleRepo;
            _roomRepo = roomRepo;
            _staffRepo = staffRepo;
            _counterService = counterService;
        }

        public async Task<ApiResponse<object>> CreateWorkScheduleAsync(
            WorkScheduleCreateRequest request)
        {
            try
            {
                var staffId = request.StaffId.Trim();

                if (!TryNormalizeShift(request.Shift, out var shift))
                {
                    return ApiResponse<object>.Fail(
                        "Shift must be Morning or Night.", 400);
                }

                var staff = await _staffRepo.GetStaffByIdAsync(staffId);

                if (staff == null)
                {
                    return ApiResponse<object>.Fail("Staff not found.", 404);
                }

                if (!IsSchedulableRole(staff.Role))
                {
                    return ApiResponse<object>.Fail(
                        "Work schedules are only available for Doctor and Pharmacist staff.", 400);
                }

                ClinicRoom? room = null;
                TimeOnly roomStart = default;
                TimeOnly roomEnd = default;
                if (string.Equals(staff.Role, "Doctor", StringComparison.OrdinalIgnoreCase))
                {
                    var roomId = request.RoomId?.Trim();
                    if (string.IsNullOrWhiteSpace(roomId))
                    {
                        return ApiResponse<object>.Fail(
                            "A clinic room is required for doctor schedules.", 400);
                    }

                    room = await _roomRepo.GetRoomByIdAsync(roomId);
                    if (room == null)
                    {
                        return ApiResponse<object>.Fail("Clinic room not found.", 404);
                    }

                    if (!string.Equals(room.Status, "Available", StringComparison.OrdinalIgnoreCase))
                    {
                        return ApiResponse<object>.Fail(
                            "Only available clinic rooms can be assigned.", 409);
                    }

                    (roomStart, roomEnd) = ShiftBounds(shift);
                    var requestedStart = request.WorkingDate.ToDateTime(roomStart);
                    var requestedEnd = request.WorkingDate.ToDateTime(roomEnd);
                    if (requestedEnd <= requestedStart)
                    {
                        requestedEnd = requestedEnd.AddDays(1);
                    }

                    var roomAssignments = await _roomRepo
                        .GetAssignmentsByRoomIdAsync(room.RoomId);
                    var roomConflict = roomAssignments.Any(item =>
                    {
                        var existingStart = item.AssignmentDate.ToDateTime(item.StartTime);
                        var existingEnd = item.AssignmentDate.ToDateTime(item.EndTime);
                        if (existingEnd <= existingStart)
                        {
                            existingEnd = existingEnd.AddDays(1);
                        }

                        return requestedStart < existingEnd && existingStart < requestedEnd;
                    });

                    if (roomConflict)
                    {
                        return ApiResponse<object>.Fail(
                            "This clinic room is already assigned during that shift.", 409);
                    }
                }

                var overlaps = await _workScheduleRepo.HasActiveScheduleOnDateAsync(
                    staffId,
                    request.WorkingDate);

                if (overlaps)
                {
                    return ApiResponse<object>.Fail(
                        "This staff member already has an active schedule on this date.", 409);
                }

                var workSchedule = new WorkSchedule
                {
                    ScheduleId = await _counterService.GenerateNextIdAsync("ScheduleId"),
                    StaffId = staffId,
                    WorkingDate = request.WorkingDate,
                    Shift = shift,
                    Status = "Active"
                };

                RoomAssignment? roomAssignment = null;
                if (room != null)
                {
                    roomAssignment = new RoomAssignment
                    {
                        AssignmentId = $"RA-{Guid.NewGuid():N}"[..11].ToUpperInvariant(),
                        ScheduleId = workSchedule.ScheduleId,
                        RoomId = room.RoomId,
                        DoctorId = staffId,
                        AssignmentDate = request.WorkingDate,
                        StartTime = roomStart,
                        EndTime = roomEnd,
                        Room = room,
                        Schedule = workSchedule,
                    };
                    await _workScheduleRepo.CreateWorkScheduleWithRoomAsync(
                        workSchedule,
                        roomAssignment);
                }
                else
                {
                    await _workScheduleRepo.CreateWorkScheduleAsync(workSchedule);
                }

                workSchedule.Staff = staff;
                return ApiResponse<object>.Ok(ToResponse(workSchedule, roomAssignment));
            }
            catch (Exception)
            {
                return ApiResponse<object>.Fail(
                    "An unexpected error occurred.", 500);
            }
        }

        public async Task<ApiResponse<object>> RetrieveAllWorkSchedulesAsync(
            long userId,
            string role)
        {
            try
            {
                var scopedStaffId = await ResolveScopedStaffIdAsync(userId, role);

                if (!IsAdminRole(role) && scopedStaffId == null)
                {
                    return ApiResponse<object>.Fail("Staff profile not found.", 404);
                }

                var schedules = scopedStaffId == null
                    ? await _workScheduleRepo.GetAllWorkSchedulesAsync()
                    : await _workScheduleRepo.GetWorkSchedulesByStaffIdAsync(scopedStaffId);

                if (schedules.Count == 0)
                {
                    return ApiResponse<object>.Fail("No work schedule found.", 404);
                }

                var assignments = await LoadRoomAssignmentsAsync(
                    schedules.Select(schedule => schedule.ScheduleId));

                return ApiResponse<object>.Ok(schedules
                    .Select(schedule => ToResponse(
                        schedule,
                        assignments.GetValueOrDefault(schedule.ScheduleId)))
                    .ToList());
            }
            catch (Exception)
            {
                return ApiResponse<object>.Fail(
                    "An unexpected error occurred.", 500);
            }
        }

        public async Task<ApiResponse<object>> RetrieveWorkScheduleByIdAsync(
            string scheduleId,
            long userId,
            string role)
        {
            try
            {
                var schedule = await _workScheduleRepo
                    .GetWorkScheduleByIdAsync(scheduleId.Trim());

                if (schedule == null)
                {
                    return ApiResponse<object>.Fail("Work schedule not found.", 404);
                }

                if (!IsSchedulableRole(schedule.Staff?.Role))
                {
                    return ApiResponse<object>.Fail(
                        "Work schedules are only available for Doctor and Pharmacist staff.", 400);
                }

                if (!await CanAccessStaffScheduleAsync(schedule.StaffId, userId, role))
                {
                    return ApiResponse<object>.Fail(
                        "You can only view your own work schedules.", 403);
                }

                var assignment = await LoadRoomAssignmentAsync(schedule.ScheduleId);
                return ApiResponse<object>.Ok(ToResponse(schedule, assignment));
            }
            catch (Exception)
            {
                return ApiResponse<object>.Fail(
                    "An unexpected error occurred.", 500);
            }
        }

        public async Task<ApiResponse<object>> RetrieveWorkSchedulesByStaffIdAsync(
            string staffId,
            long userId,
            string role)
        {
            try
            {
                var normalizedStaffId = staffId.Trim();

                if (!await CanAccessStaffScheduleAsync(normalizedStaffId, userId, role))
                {
                    return ApiResponse<object>.Fail(
                        "You can only view your own work schedules.", 403);
                }

                var staff = await _staffRepo.GetStaffByIdAsync(normalizedStaffId);

                if (staff == null)
                {
                    return ApiResponse<object>.Fail("Staff not found.", 404);
                }

                var schedules = await _workScheduleRepo
                    .GetWorkSchedulesByStaffIdAsync(normalizedStaffId);

                if (schedules.Count == 0)
                {
                    return ApiResponse<object>.Fail(
                        "No work schedule found for this staff member.", 404);
                }

                var assignments = await LoadRoomAssignmentsAsync(
                    schedules.Select(schedule => schedule.ScheduleId));

                return ApiResponse<object>.Ok(schedules
                    .Select(schedule => ToResponse(
                        schedule,
                        assignments.GetValueOrDefault(schedule.ScheduleId)))
                    .ToList());
            }
            catch (Exception)
            {
                return ApiResponse<object>.Fail(
                    "An unexpected error occurred.", 500);
            }
        }

        public async Task<ApiResponse<object>> UpdateWorkScheduleAsync(
            WorkScheduleUpdateRequest request)
        {
            try
            {
                var schedule = await _workScheduleRepo
                    .GetWorkScheduleByIdAsync(request.ScheduleId.Trim());

                if (schedule == null)
                {
                    return ApiResponse<object>.Fail("Work schedule not found.", 404);
                }

                if (!IsSchedulableRole(schedule.Staff?.Role))
                {
                    return ApiResponse<object>.Fail(
                        "Work schedules are only available for Doctor and Pharmacist staff.", 400);
                }

                var workingDate = request.WorkingDate ?? schedule.WorkingDate;
                var shift = schedule.Shift;
                var status = request.Status?.Trim() ?? schedule.Status;

                if (!string.IsNullOrWhiteSpace(request.Shift))
                {
                    if (!TryNormalizeShift(request.Shift, out shift))
                    {
                        return ApiResponse<object>.Fail(
                            "Shift must be Morning or Night.", 400);
                    }
                }

                if (!ValidStatuses.Contains(status))
                {
                    return ApiResponse<object>.Fail(
                        "Status must be Active, Inactive, or Room Reassignment Required.", 400);
                }

                if (status == "Active")
                {
                    var overlaps = await _workScheduleRepo.HasActiveScheduleOnDateAsync(
                        schedule.StaffId,
                        workingDate,
                        schedule.ScheduleId);

                    if (overlaps)
                    {
                        return ApiResponse<object>.Fail(
                            "This staff member already has an active schedule on this date.", 409);
                    }
                }

                var roomAssignment = await LoadRoomAssignmentAsync(
                    schedule.ScheduleId,
                    trackChanges: true);
                if (status == "Active"
                    && string.Equals(schedule.Staff?.Role, "Doctor", StringComparison.OrdinalIgnoreCase)
                    && roomAssignment == null)
                {
                    return ApiResponse<object>.Fail(
                        "A clinic room is required before activating a doctor schedule.", 400);
                }

                if (roomAssignment != null)
                {
                    var (startTime, endTime) = ShiftBounds(shift);
                    var requestedStart = workingDate.ToDateTime(startTime);
                    var requestedEnd = workingDate.ToDateTime(endTime);
                    if (requestedEnd <= requestedStart)
                    {
                        requestedEnd = requestedEnd.AddDays(1);
                    }

                    var conflictingAssignments = await _roomRepo
                        .GetAssignmentsByRoomIdAsync(roomAssignment.RoomId);

                    var roomConflict = conflictingAssignments.Any(item =>
                    {
                        var existingStart = item.AssignmentDate.ToDateTime(item.StartTime);
                        var existingEnd = item.AssignmentDate.ToDateTime(item.EndTime);
                        if (existingEnd <= existingStart)
                        {
                            existingEnd = existingEnd.AddDays(1);
                        }

                        return requestedStart < existingEnd && existingStart < requestedEnd;
                    });

                    if (roomConflict)
                    {
                        return ApiResponse<object>.Fail(
                            "The assigned clinic room is already in use during that shift.", 409);
                    }

                    roomAssignment.AssignmentDate = workingDate;
                    roomAssignment.StartTime = startTime;
                    roomAssignment.EndTime = endTime;
                }

                schedule.WorkingDate = workingDate;
                schedule.Shift = shift;
                schedule.Status = status;

                await _workScheduleRepo.UpdateWorkScheduleAsync(schedule);

                return ApiResponse<object>.Ok(ToResponse(schedule, roomAssignment));
            }
            catch (Exception)
            {
                return ApiResponse<object>.Fail(
                    "An unexpected error occurred.", 500);
            }
        }

        private static WorkScheduleRetrieveResponse ToResponse(
            WorkSchedule workSchedule,
            RoomAssignment? roomAssignment)
        {
            return new WorkScheduleRetrieveResponse
            {
                ScheduleId = workSchedule.ScheduleId,
                StaffId = workSchedule.StaffId,
                StaffName = workSchedule.Staff?.User?.Name
                    ?? workSchedule.Staff?.User?.Username
                    ?? string.Empty,
                Username = workSchedule.Staff?.User?.Username ?? string.Empty,
                Role = workSchedule.Staff?.Role ?? string.Empty,
                WorkingDate = workSchedule.WorkingDate,
                Shift = workSchedule.Shift,
                Status = workSchedule.Status,
                RoomAssignmentId = roomAssignment?.AssignmentId,
                RoomId = roomAssignment?.RoomId,
                RoomName = roomAssignment?.Room?.RoomName,
            };
        }

        private async Task<RoomAssignment?> LoadRoomAssignmentAsync(
            string scheduleId,
            bool trackChanges = false)
        {
            return await _roomRepo.GetAssignmentByScheduleIdAsync(
                scheduleId,
                trackChanges);
        }

        private async Task<Dictionary<string, RoomAssignment>> LoadRoomAssignmentsAsync(
            IEnumerable<string> scheduleIds)
        {
            var ids = scheduleIds
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (ids.Count == 0)
            {
                return new Dictionary<string, RoomAssignment>(StringComparer.OrdinalIgnoreCase);
            }

            var assignments = await _roomRepo
                .GetRoomAssignmentsByScheduleIdsAsync(ids);

            return assignments
                .Where(assignment => assignment.ScheduleId != null)
                .ToDictionary(
                    assignment => assignment.ScheduleId!,
                    StringComparer.OrdinalIgnoreCase);
        }

        private static bool TryNormalizeShift(
            string? shift,
            out string normalizedShift)
        {
            if (!string.IsNullOrWhiteSpace(shift) && ValidShifts.Contains(shift.Trim()))
            {
                normalizedShift = char.ToUpperInvariant(shift.Trim()[0])
                    + shift.Trim()[1..].ToLowerInvariant();
                return true;
            }

            normalizedShift = string.Empty;
            return false;
        }

        private static bool IsSchedulableRole(string? role)
        {
            return string.Equals(role, "Doctor", StringComparison.OrdinalIgnoreCase)
                || string.Equals(role, "Pharmacist", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsAdminRole(string? role) =>
            string.Equals(role, "Admin", StringComparison.OrdinalIgnoreCase);

        private static (TimeOnly Start, TimeOnly End) ShiftBounds(string shift)
        {
            return string.Equals(shift, "Night", StringComparison.OrdinalIgnoreCase)
                ? (new TimeOnly(18, 0), new TimeOnly(6, 0))
                : (new TimeOnly(6, 0), new TimeOnly(18, 0));
        }

        private async Task<string?> ResolveScopedStaffIdAsync(
            long userId,
            string role)
        {
            if (IsAdminRole(role))
            {
                return null;
            }

            var staff = await _staffRepo.GetStaffByUserIdAsync(userId);
            return staff != null && IsSchedulableRole(staff.Role)
                ? staff.StaffId
                : null;
        }

        private async Task<bool> CanAccessStaffScheduleAsync(
            string staffId,
            long userId,
            string role)
        {
            if (IsAdminRole(role))
            {
                return true;
            }

            var scopedStaffId = await ResolveScopedStaffIdAsync(userId, role);
            return scopedStaffId != null
                && string.Equals(scopedStaffId, staffId, StringComparison.OrdinalIgnoreCase);
        }
    }
}
