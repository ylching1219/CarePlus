using CarePlus.API.DTO.InvoiceDTO;
using CarePlus.API.Services.InvoiceServices.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
// PayAsync stays on IInvoiceServices for Stripe Checkout/Complete only.

namespace CarePlus.API.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/[controller]")]
    public class InvoiceController : ControllerBase
    {
        private readonly IInvoiceServices _invoices;

        public InvoiceController(IInvoiceServices invoices)
        {
            _invoices = invoices;
        }

        [HttpGet("Outstanding")]
        public async Task<IActionResult> Outstanding()
        {
            if (!long.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
            {
                return Unauthorized(ApiResponse<List<OutstandingInvoiceResponse>>.Fail("Invalid token.", 401));
            }

            return Ok(await _invoices.ListOutstandingAsync(userId));
        }

        [HttpGet("History")]
        public async Task<IActionResult> History()
        {
            if (!long.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
            {
                return Unauthorized(ApiResponse<List<PaidInvoiceResponse>>.Fail("Invalid token.", 401));
            }

            return Ok(await _invoices.ListPaidAsync(userId));
        }

        // Patients pay through Stripe Checkout only (`POST /api/Stripe/Checkout/Complete`).
        // Keeping PayAsync for that server-side path; do not re-expose a client Pay endpoint.
    }
}
