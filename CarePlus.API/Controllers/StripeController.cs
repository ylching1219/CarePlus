using CarePlus.API.DTO.StripeDTO;
using CarePlus.API.Services.StripeServices.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CarePlus.API.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/[controller]")]
    public class StripeController : ControllerBase
    {
        private readonly IStripeCheckoutServices _stripe;

        public StripeController(IStripeCheckoutServices stripe)
        {
            _stripe = stripe;
        }

        [HttpPost("Checkout/Invoice")]
        public async Task<IActionResult> CheckoutInvoice(CreateInvoiceCheckoutRequest request)
        {
            if (!long.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
            {
                return Unauthorized(ApiResponse<CheckoutSessionResponse>.Fail("Invalid token.", 401));
            }

            return Ok(await _stripe.CreateInvoiceCheckoutAsync(userId, request));
        }

        [HttpPost("Checkout/Medicine")]
        public async Task<IActionResult> CheckoutMedicine(CreateMedicineCheckoutRequest request)
        {
            if (!long.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
            {
                return Unauthorized(ApiResponse<CheckoutSessionResponse>.Fail("Invalid token.", 401));
            }

            return Ok(await _stripe.CreateMedicineCheckoutAsync(userId, request));
        }

        [HttpPost("Checkout/Complete")]
        public async Task<IActionResult> Complete(CompleteCheckoutRequest request)
        {
            if (!long.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
            {
                return Unauthorized(ApiResponse<CompletedCheckoutResponse>.Fail("Invalid token.", 401));
            }

            return Ok(await _stripe.CompleteCheckoutAsync(userId, request));
        }
    }
}
