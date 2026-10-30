using CarePlus.API.DTO.LeaveDTO;
using CarePlus.API.Extension;
using CarePlus.API.Models.Leave;
using CarePlus.API.Repository.Interface;
using CarePlus.API.Services.LeaveServices.Interface;

namespace CarePlus.API.Services.LeaveServices
{
    public class LeaveServices : ILeaveServices
    {
        private static readonly HashSet<string> ValidStatuses =
            new(StringComparer.Ordinal)
            {
                "Pending",
                "Approved",
                "Declined"
            };

        private readonly ILeaveRepository _leaveRepo;
        private readonly IStaffRepository _staffRepo;
        private readonly IWorkScheduleRepository _workScheduleRepo;

        public LeaveServices(
            ILeaveRepository leaveRepo,
            IStaffRepository staffRepo,
            IWorkScheduleRepository workScheduleRepo)
        {
            _leaveRepo = leaveRepo;
            _staffRepo = staffRepo;
            _workScheduleRepo = workScheduleRepo;
        }

        public async Task<ApiResponse<object>> CreateLeaveAsync(
            LeaveCreateRequest request)
        {
            try
            {
                var staffId = request.StaffId.Trim();
                var startDate = request.StartDate.Date;
                var endDate = request.EndDate.Date;
                var reason = request.Reason.Trim();
                var supportDocument = request.SupportDocument.Trim();

                if (string.IsNullOrWhiteSpace(supportDocument))
                {
                    return ApiResponse<object>.Fail(
                        "Support document is required.", 400);
                }

                if (startDate > endDate)
                {
                    return ApiResponse<object>.Fail(
                        "End date must be the same as or later than start date.",
                        400);
                }

                if (string.IsNullOrWhiteSpace(reason))
                {
                    return ApiResponse<object>.Fail(
                        "Reason is required.", 400);
                }

                var staff = await _staffRepo.GetStaffByIdAsync(staffId);

                if (staff == null)
                {
                    return ApiResponse<object>.Fail("Staff not found.", 404);
                }

                var overlaps = await _leaveRepo.HasOverlappingLeaveAsync(
                    staffId,
                    startDate,
                    endDate);

                if (overlaps)
                {
                    return ApiResponse<object>.Fail(
                        "This staff member already has a pending or approved leave during these dates.",
                        409);
                }

                var leave = new Leave
                {
                    LeaveId = CreateLeaveId(),
                    StaffId = staffId,
                    StartDate = startDate,
                    EndDate = endDate,
                    Reason = reason,
                    SupportDocument = supportDocument,
                    Status = "Pending",
                    Staff = staff
                };

                await _leaveRepo.CreateLeaveAsync(leave);

                return ApiResponse<object>.Ok(ToResponse(leave));
            }
            catch (Exception ex)
            {
                return ApiResponse<object>.Fail(
                    $"An error occurred: {ex.Message}", 500);
            }
        }

        public async Task<ApiResponse<object>> RetrieveAllLeavesAsync()
        {
            try
            {
                var leaves = await _leaveRepo.GetAllLeavesAsync();

                if (leaves.Count == 0)
                {
                    return ApiResponse<object>.Fail(
                        "No leave application found.", 404);
                }

                return ApiResponse<object>.Ok(
                    leaves.Select(ToResponse).ToList());
            }
            catch (Exception ex)
            {
                return ApiResponse<object>.Fail(
                    $"An error occurred: {ex.Message}", 500);
            }
        }

        public async Task<ApiResponse<object>> RetrieveLeaveByIdAsync(
            string leaveId)
        {
            try
            {
                var leave = await _leaveRepo.GetLeaveByIdAsync(leaveId.Trim());

                if (leave == null)
                {
                    return ApiResponse<object>.Fail(
                        "Leave application not found.", 404);
                }

                return ApiResponse<object>.Ok(ToResponse(leave));
            }
            catch (Exception ex)
            {
                return ApiResponse<object>.Fail(
                    $"An error occurred: {ex.Message}", 500);
            }
        }

        public async Task<ApiResponse<object>> RetrieveLeavesByStaffIdAsync(
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

                var leaves = await _leaveRepo
                    .GetLeavesByStaffIdAsync(normalizedStaffId);

                if (leaves.Count == 0)
                {
                    return ApiResponse<object>.Fail(
                        "No leave application found for this staff member.",
                        404);
                }

                return ApiResponse<object>.Ok(
                    leaves.Select(ToResponse).ToList());
            }
            catch (Exception ex)
            {
                return ApiResponse<object>.Fail(
                    $"An error occurred: {ex.Message}", 500);
            }
        }

        public async Task<ApiResponse<object>> UpdateLeaveStatusAsync(
            LeaveStatusUpdateRequest request)
        {
            try
            {
                var leave = await _leaveRepo
                    .GetLeaveByIdAsync(request.LeaveId.Trim());

                if (leave == null)
                {
                    return ApiResponse<object>.Fail(
                        "Leave application not found.", 404);
                }

                var status = request.Status.Trim();

                if (!ValidStatuses.Contains(status))
                {
                    return ApiResponse<object>.Fail(
                        "Status must be Pending, Approved, or Declined.",
                        400);
                }

                if (status == "Approved")
                {
                    var overlaps = await _leaveRepo.HasOverlappingLeaveAsync(
                        leave.StaffId,
                        leave.StartDate,
                        leave.EndDate,
                        leave.LeaveId);

                    if (overlaps)
                    {
                        return ApiResponse<object>.Fail(
                            "This staff member already has another pending or approved leave during these dates.",
                            409);
                    }

                    await _workScheduleRepo.DeactivateActiveSchedulesAsync(
                        leave.StaffId,
                        DateOnly.FromDateTime(leave.StartDate.Date),
                        DateOnly.FromDateTime(leave.EndDate.Date));
                }

                leave.Status = status;
                await _leaveRepo.UpdateLeaveAsync(leave);

                return ApiResponse<object>.Ok(ToResponse(leave));
            }
            catch (Exception ex)
            {
                return ApiResponse<object>.Fail(
                    $"An error occurred: {ex.Message}", 500);
            }
        }

        private static LeaveRetrieveResponse ToResponse(Leave leave)
        {
            return new LeaveRetrieveResponse
            {
                LeaveId = leave.LeaveId,
                StaffId = leave.StaffId,
                StaffName = leave.Staff?.User?.Name
                    ?? leave.Staff?.User?.Username
                    ?? string.Empty,
                Username = leave.Staff?.User?.Username ?? string.Empty,
                Role = leave.Staff?.Role ?? string.Empty,
                StartDate = leave.StartDate,
                EndDate = leave.EndDate,
                DurationDays = (leave.EndDate.Date - leave.StartDate.Date).Days + 1,
                Reason = leave.Reason,
                SupportDocument = leave.SupportDocument,
                CreatedAt = leave.CreatedAt,
                Status = leave.Status
            };
        }

        private static string CreateLeaveId()
        {
            var datePart = DateTimeHelper.MalaysiaNow().ToString("yyMMdd");
            var uniquePart = Guid.NewGuid()
                .ToString("N")[..8]
                .ToUpperInvariant();

            return $"LV-{datePart}-{uniquePart}";
        }
    }
}
