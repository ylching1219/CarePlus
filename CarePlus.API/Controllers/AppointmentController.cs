using CarePlus.API.DTO.AppointmentDTO;
using CarePlus.API.Services.AppointmentServices.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CarePlus.API.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/[controller]")]
    public class AppointmentController : ControllerBase
    {
        private readonly IAppointmentServices _appointments;

        public AppointmentController(IAppointmentServices appointments)
        {
            _appointments = appointments;
        }

        [HttpGet("Catalog")]
        public async Task<IActionResult> Catalog()
        {
            if (!TryUserId(out var userId))
            {
                return Unauthorized(ApiResponse<BookingCatalogResponse>.Fail("Invalid token.", 401));
            }

            return Ok(await _appointments.GetCatalogAsync(userId));
        }

        [HttpGet("Slots")]
        public async Task<IActionResult> Slots([FromQuery] string date, [FromQuery] string doctorId)
        {
            if (!TryUserId(out var userId))
            {
                return Unauthorized(ApiResponse<BookingSlotsResponse>.Fail("Invalid token.", 401));
            }

            return Ok(await _appointments.GetSlotsAsync(userId, date, doctorId));
        }

        [HttpGet("Upcoming")]
        public async Task<IActionResult> Upcoming()
        {
            if (!TryUserId(out var userId))
            {
                return Unauthorized(ApiResponse<List<AppointmentResponse>>.Fail("Invalid token.", 401));
            }

            return Ok(await _appointments.ListUpcomingAsync(userId));
        }

        [HttpGet("History")]
        public async Task<IActionResult> History()
        {
            if (!TryUserId(out var userId))
            {
                return Unauthorized(ApiResponse<List<AppointmentResponse>>.Fail("Invalid token.", 401));
            }

            return Ok(await _appointments.ListHistoryAsync(userId));
        }

        [HttpPost]
        public async Task<IActionResult> Book([FromBody] AppointmentBookRequest request)
        {
            if (!TryUserId(out var userId))
            {
                return Unauthorized(ApiResponse<AppointmentResponse>.Fail("Invalid token.", 401));
            }

            return Ok(await _appointments.BookAsync(userId, request));
        }

        [HttpPost("{id}/Cancel")]
        public async Task<IActionResult> Cancel(string id)
        {
            if (!TryUserId(out var userId))
            {
                return Unauthorized(ApiResponse<AppointmentResponse>.Fail("Invalid token.", 401));
            }

            return Ok(await _appointments.CancelAsync(userId, id));
        }

        [HttpPost("{id}/Reschedule")]
        public async Task<IActionResult> Reschedule(string id, [FromBody] AppointmentRescheduleRequest request)
        {
            if (!TryUserId(out var userId))
            {
                return Unauthorized(ApiResponse<AppointmentResponse>.Fail("Invalid token.", 401));
            }

            return Ok(await _appointments.RescheduleAsync(userId, id, request));
        }

        private bool TryUserId(out long userId) =>
            long.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out userId);
    }
}
