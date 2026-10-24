using CarePlus.API.DTO.InvoiceDTO;
using CarePlus.API.Services.InvoiceServices.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

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

        [HttpPost("{id}/Pay")]
        public async Task<IActionResult> Pay(string id, InvoicePayRequest request)
        {
            if (!long.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
            {
                return Unauthorized(ApiResponse<OutstandingInvoiceResponse>.Fail("Invalid token.", 401));
            }

            return Ok(await _invoices.PayAsync(userId, id, request));
        }
    }
}
