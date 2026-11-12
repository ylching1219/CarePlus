using CarePlus.API.DTO.QueueDTO;
using CarePlus.API.Services.QueueServices.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CarePlus.API.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/[controller]")]
    public class QueueController : ControllerBase
    {
        private readonly IQueueServices _queue;

        public QueueController(IQueueServices queue)
        {
            _queue = queue;
        }

        [HttpGet]
        public async Task<IActionResult> Overview([FromQuery] string? appointmentId)
        {
            if (!TryUserId(out var userId)) return Unauthorized(ApiResponse<QueueOverviewResponse>.Fail("Invalid token.", 401));
            return Ok(await _queue.GetOverviewAsync(userId, appointmentId));
        }

        [HttpPost("check-in/{appointmentId}")]
        public async Task<IActionResult> CheckIn(string appointmentId)
        {
            if (!TryUserId(out var userId)) return Unauthorized(ApiResponse<QueueStatusResponse>.Fail("Invalid token.", 401));
            return Ok(await _queue.CheckInAsync(userId, appointmentId));
        }

        [Authorize(Roles = "Admin,Doctor")]
        [HttpGet("staff")]
        public async Task<IActionResult> StaffQueue([FromQuery] DateOnly? date)
        {
            if (!TryUserId(out var userId)) return Unauthorized(ApiResponse<List<StaffQueueEntryResponse>>.Fail("Invalid token.", 401));
            return Ok(await _queue.GetStaffQueueAsync(userId, User.FindFirstValue(ClaimTypes.Role) ?? "", date));
        }

        [Authorize(Roles = "Admin,Doctor")]
        [HttpPatch("{queueId}/status")]
        public async Task<IActionResult> UpdateStatus(string queueId, [FromBody] QueueStatusUpdateRequest request)
        {
            if (!TryUserId(out var userId)) return Unauthorized(ApiResponse<QueueStatusResponse>.Fail("Invalid token.", 401));
            return Ok(await _queue.UpdateStatusAsync(userId, User.FindFirstValue(ClaimTypes.Role) ?? "", queueId, request.Status, request.ExpectedStatus));
        }

        [Authorize(Roles = "Doctor")]
        [HttpPost("{queueId}/consultation/complete")]
        public async Task<IActionResult> CompleteConsultation(string queueId, QueueConsultationRequest request)
        {
            if (!TryUserId(out var userId)) return Unauthorized(ApiResponse<QueueStatusResponse>.Fail("Invalid token.", 401));
            return Ok(await _queue.CompleteConsultationAsync(userId, queueId, request));
        }

        private bool TryUserId(out long userId) => long.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out userId);
    }
}
