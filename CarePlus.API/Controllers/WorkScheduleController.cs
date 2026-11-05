using CarePlus.API.DTO.WorkScheduleDTO;
using CarePlus.API.Services.WorkScheduleServices.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CarePlus.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin,Doctor,Pharmacist")]
    public class WorkScheduleController : ControllerBase
    {
        private readonly IWorkScheduleServices _workScheduleServices;

        public WorkScheduleController(IWorkScheduleServices workScheduleServices)
        {
            _workScheduleServices = workScheduleServices;
        }

        [HttpPost("Create")]
        public async Task<IActionResult> CreateWorkSchedule(
            WorkScheduleCreateRequest request)
        {
            return Ok(await _workScheduleServices
                .CreateWorkScheduleAsync(request));
        }

        [HttpGet("Retrieve")]
        public async Task<IActionResult> RetrieveWorkSchedules()
        {
            if (!TryGetUserContext(out var userId, out var role))
            {
                return Unauthorized(ApiResponse<object>.Fail("Invalid token.", 401));
            }

            return Ok(await _workScheduleServices
                .RetrieveAllWorkSchedulesAsync(userId, role));
        }

        [HttpGet("RetrieveById/{scheduleId}")]
        public async Task<IActionResult> RetrieveWorkScheduleById(string scheduleId)
        {
            if (!TryGetUserContext(out var userId, out var role))
            {
                return Unauthorized(ApiResponse<object>.Fail("Invalid token.", 401));
            }

            return Ok(await _workScheduleServices
                .RetrieveWorkScheduleByIdAsync(scheduleId, userId, role));
        }

        [HttpGet("RetrieveByStaffId/{staffId}")]
        public async Task<IActionResult> RetrieveWorkSchedulesByStaffId(string staffId)
        {
            if (!TryGetUserContext(out var userId, out var role))
            {
                return Unauthorized(ApiResponse<object>.Fail("Invalid token.", 401));
            }

            return Ok(await _workScheduleServices
                .RetrieveWorkSchedulesByStaffIdAsync(staffId, userId, role));
        }

        [HttpPut("Update")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateWorkSchedule(
            WorkScheduleUpdateRequest request)
        {
            return Ok(await _workScheduleServices
                .UpdateWorkScheduleAsync(request));
        }

        private bool TryGetUserContext(out long userId, out string role)
        {
            role = User.FindFirstValue(ClaimTypes.Role) ?? string.Empty;
            return long.TryParse(
                User.FindFirstValue(ClaimTypes.NameIdentifier),
                out userId);
        }
    }
}
