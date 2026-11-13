using CarePlus.API.Data;
using CarePlus.API.DTO.WorkScheduleDTO;
using CarePlus.API.Models.RoomAssg;
using CarePlus.API.Models.WorkSch;
using CarePlus.API.Repository.Interface;
using CarePlus.API.Services.CounterServices.Interface;
using CarePlus.API.Services.WorkScheduleServices.Interface;
using Microsoft.EntityFrameworkCore;

namespace CarePlus.API.Services.WorkScheduleServices
{
    public class WorkScheduleServices : IWorkScheduleServices
    {
        private static readonly HashSet<string> ValidStatuses =
            new(StringComparer.Ordinal)
            {
                "Active",
                "Inactive"
            };

        private static readonly HashSet<string> ValidShifts =
            new(StringComparer.OrdinalIgnoreCase)
            {
                "Morning",
                "Night"
            };

        private readonly IWorkScheduleRepository _workScheduleRepo;
        private readonly IStaffRepository _staffRepo;
        private readonly ICounterServices _counterService;
        private readonly AppDbContext _db;

        public WorkScheduleServices(
            IWorkScheduleRepository workScheduleRepo,
            IStaffRepository staffRepo,
            ICounterServices counterService,
            AppDbContext db)
        {
            _workScheduleRepo = workScheduleRepo;
            _staffRepo = staffRepo;
            _counterService = counterService;
            _db = db;
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

                await _workScheduleRepo.CreateWorkScheduleAsync(workSchedule);

                workSchedule.Staff = staff;
                return ApiResponse<object>.Ok(ToResponse(workSchedule, null));
            }
            catch (Exception ex)
            {
                return ApiResponse<object>.Fail(
                    $"An error occurred: {ex.Message}", 500);
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
            catch (Exception ex)
            {
                return ApiResponse<object>.Fail(
                    $"An error occurred: {ex.Message}", 500);
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
            catch (Exception ex)
            {
                return ApiResponse<object>.Fail(
                    $"An error occurred: {ex.Message}", 500);
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
            catch (Exception ex)
            {
                return ApiResponse<object>.Fail(
                    $"An error occurred: {ex.Message}", 500);
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
                        "Status must be Active or Inactive.", 400);
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
                if (roomAssignment != null)
                {
                    var (startTime, endTime) = ShiftBounds(shift);
                    var requestedStart = workingDate.ToDateTime(startTime);
                    var requestedEnd = workingDate.ToDateTime(endTime);
                    if (requestedEnd <= requestedStart)
                    {
                        requestedEnd = requestedEnd.AddDays(1);
                    }

                    var conflictingAssignments = await _db.RoomAssignments
                        .AsNoTracking()
                        .Where(item => item.RoomId == roomAssignment.RoomId
                            && item.AssignmentId != roomAssignment.AssignmentId)
                        .ToListAsync();

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
            catch (Exception ex)
            {
                return ApiResponse<object>.Fail(
                    $"An error occurred: {ex.Message}", 500);
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
            var query = _db.RoomAssignments
                .Include(assignment => assignment.Room)
                .Where(assignment => assignment.ScheduleId == scheduleId);

            if (!trackChanges)
            {
                query = query.AsNoTracking();
            }

            return await query.FirstOrDefaultAsync();
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

            var assignments = await _db.RoomAssignments
                .AsNoTracking()
                .Include(assignment => assignment.Room)
                .Where(assignment => assignment.ScheduleId != null
                    && ids.Contains(assignment.ScheduleId))
                .ToListAsync();

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
