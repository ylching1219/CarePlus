using CarePlus.API.DTO.CartDTO;
using CarePlus.API.Services.CartServices.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CarePlus.API.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/[controller]")]
    public class CartController : ControllerBase
    {
        private readonly ICartServices _cart;

        public CartController(ICartServices cart)
        {
            _cart = cart;
        }

        [HttpGet]
        public Task<IActionResult> List()
        {
            return ForPatient(userId => _cart.ListAsync(userId));
        }

        [HttpPut]
        public Task<IActionResult> SetQty(CartQtyRequest request)
        {
            return ForPatient(userId => _cart.SetQtyAsync(userId, request));
        }

        [HttpPost("Remove")]
        public Task<IActionResult> Remove(CartRemoveRequest request)
        {
            return ForPatient(userId => _cart.RemoveAsync(userId, request.MedicineIds ?? []));
        }

        [HttpDelete]
        public Task<IActionResult> Clear()
        {
            return ForPatient(userId => _cart.ClearAsync(userId));
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
