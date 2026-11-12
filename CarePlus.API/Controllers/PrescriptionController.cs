using System.Security.Claims;
using CarePlus.API.DTO.PrescriptionDTO;
using CarePlus.API.Services.PrescriptionServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CarePlus.API.Controllers;

[ApiController]
[Authorize(Roles = "Doctor,Pharmacist")]
[Route("api/[controller]")]
public class PrescriptionController(PrescriptionServices prescriptions) : ControllerBase
{
    [HttpGet("Retrieve")]
    public async Task<IActionResult> Retrieve(CancellationToken cancellationToken)
    {
        if (!TryUserId(out var userId))
        {
            return Unauthorized(ApiResponse<List<PrescriptionSummaryResponse>>.Fail("Invalid token.", 401));
        }

        var rows = await prescriptions.RetrieveAsync(userId, User.IsInRole("Pharmacist"), cancellationToken);
        return Ok(ApiResponse<List<PrescriptionSummaryResponse>>.Ok(rows));
    }

    [HttpGet("{prescriptionId}/Items")]
    public async Task<IActionResult> RetrieveItems(string prescriptionId, CancellationToken cancellationToken)
    {
        if (!TryUserId(out var userId))
        {
            return Unauthorized(ApiResponse<List<PrescriptionItemResponse>>.Fail("Invalid token.", 401));
        }

        var items = await prescriptions.RetrieveItemsAsync(userId, User.IsInRole("Pharmacist"), prescriptionId, cancellationToken);
        return items == null
            ? NotFound(ApiResponse<List<PrescriptionItemResponse>>.Fail("Prescription not found.", 404))
            : Ok(ApiResponse<List<PrescriptionItemResponse>>.Ok(items));
    }

    private bool TryUserId(out long userId) =>
        long.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out userId);
}
