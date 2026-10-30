using CarePlus.API.DTO.WorkScheduleDTO;
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

        public WorkScheduleServices(
            IWorkScheduleRepository workScheduleRepo,
            IStaffRepository staffRepo,
            ICounterServices counterService)
        {
            _workScheduleRepo = workScheduleRepo;
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
                return ApiResponse<object>.Ok(ToResponse(workSchedule));
            }
            catch (Exception ex)
            {
                return ApiResponse<object>.Fail(
                    $"An error occurred: {ex.Message}", 500);
            }
        }

        public async Task<ApiResponse<object>> RetrieveAllWorkSchedulesAsync()
        {
            try
            {
                var schedules = await _workScheduleRepo.GetAllWorkSchedulesAsync();

                if (schedules.Count == 0)
                {
                    return ApiResponse<object>.Fail("No work schedule found.", 404);
                }

                return ApiResponse<object>.Ok(schedules.Select(ToResponse).ToList());
            }
            catch (Exception ex)
            {
                return ApiResponse<object>.Fail(
                    $"An error occurred: {ex.Message}", 500);
            }
        }

        public async Task<ApiResponse<object>> RetrieveWorkScheduleByIdAsync(
            string scheduleId)
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

                return ApiResponse<object>.Ok(ToResponse(schedule));
            }
            catch (Exception ex)
            {
                return ApiResponse<object>.Fail(
                    $"An error occurred: {ex.Message}", 500);
            }
        }

        public async Task<ApiResponse<object>> RetrieveWorkSchedulesByStaffIdAsync(
            string staffId)
        {
            try
            {
                var normalizedStaffId = staffId.Trim();
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

                return ApiResponse<object>.Ok(schedules.Select(ToResponse).ToList());
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

                schedule.WorkingDate = workingDate;
                schedule.Shift = shift;
                schedule.Status = status;

                await _workScheduleRepo.UpdateWorkScheduleAsync(schedule);

                return ApiResponse<object>.Ok(ToResponse(schedule));
            }
            catch (Exception ex)
            {
                return ApiResponse<object>.Fail(
                    $"An error occurred: {ex.Message}", 500);
            }
        }

        private static WorkScheduleRetrieveResponse ToResponse(
            WorkSchedule workSchedule)
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
                Status = workSchedule.Status
            };
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
    }
}
