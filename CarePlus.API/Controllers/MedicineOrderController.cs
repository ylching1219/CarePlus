using CarePlus.API.DTO.MedicineOrderDTO;
using CarePlus.API.Services.MedicineOrderServices.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CarePlus.API.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/[controller]")]
    public class MedicineOrderController : ControllerBase
    {
        private readonly IMedicineOrderServices _orders;

        public MedicineOrderController(IMedicineOrderServices orders)
        {
            _orders = orders;
        }

        [HttpGet]
        public Task<IActionResult> List()
        {
            return ForPatient(userId => _orders.ListAsync(userId));
        }

        [HttpPost]
        public Task<IActionResult> Place(MedicineOrderPlaceRequest request)
        {
            return ForPatient(userId => _orders.PlaceAsync(userId, request));
        }

        [HttpGet("Locate")]
        public async Task<IActionResult> Locate([FromQuery] string? address)
        {
            return Ok(await _orders.LocateAsync(address));
        }

        private async Task<IActionResult> ForPatient<T>(Func<long, Task<ApiResponse<T>>> call)
        {
            if (!long.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
            {
                return Unauthorized(ApiResponse<T>.Fail("Invalid token.", 401));
            }

            return Ok(await call(userId));
        }
    }
}
