using CarePlus.API.Services.MedicineOrderServices.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CarePlus.API.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/[controller]")]
    public class PaymentController : ControllerBase
    {
        private readonly IMedicineOrderServices _orders;

        public PaymentController(IMedicineOrderServices orders)
        {
            _orders = orders;
        }

        [HttpGet]
        public async Task<IActionResult> List()
        {
            if (!long.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
            {
                return Unauthorized(ApiResponse<List<DTO.MedicineOrderDTO.MedicineOrderResponse>>.Fail("Invalid token.", 401));
            }

            return Ok(await _orders.ListPaidAsync(userId));
        }
    }
}
