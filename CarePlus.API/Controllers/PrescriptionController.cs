using System.Security.Claims;
using CarePlus.API.DTO.PrescriptionDTO;
using CarePlus.API.Services.PrescriptionServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CarePlus.API.Controllers;

[ApiController]
[Authorize(Roles = "Admin,Doctor,Pharmacist")]
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

        var rows = await prescriptions.RetrieveAsync(userId, User.IsInRole("Pharmacist"), User.IsInRole("Admin"), cancellationToken);
        return Ok(ApiResponse<List<PrescriptionSummaryResponse>>.Ok(rows));
    }

    [HttpGet("{prescriptionId}/Items")]
    public async Task<IActionResult> RetrieveItems(string prescriptionId, CancellationToken cancellationToken)
    {
        if (!TryUserId(out var userId))
        {
            return Unauthorized(ApiResponse<List<PrescriptionItemResponse>>.Fail("Invalid token.", 401));
        }

        var items = await prescriptions.RetrieveItemsAsync(userId, User.IsInRole("Pharmacist"), User.IsInRole("Admin"), prescriptionId, cancellationToken);
        return items == null
            ? NotFound(ApiResponse<List<PrescriptionItemResponse>>.Fail("Prescription not found.", 404))
            : Ok(ApiResponse<List<PrescriptionItemResponse>>.Ok(items));
    }

    [Authorize(Roles = "Pharmacist")]
    [HttpPatch("{prescriptionId}/ReadyForPickup")]
    public async Task<IActionResult> ReadyForPickup(string prescriptionId, CancellationToken cancellationToken)
    {
        if (!TryUserId(out var userId))
        {
            return Unauthorized(ApiResponse<object>.Fail("Invalid token.", 401));
        }

        return Ok(await prescriptions.MarkReadyForPickupAsync(userId, prescriptionId, cancellationToken));
    }

    [Authorize(Roles = "Pharmacist")]
    [HttpPatch("{prescriptionId}/Complete")]
    public async Task<IActionResult> Complete(string prescriptionId, CancellationToken cancellationToken)
    {
        if (!TryUserId(out var userId))
        {
            return Unauthorized(ApiResponse<object>.Fail("Invalid token.", 401));
        }

        return Ok(await prescriptions.CompletePrescriptionAsync(userId, prescriptionId, cancellationToken));
    }

    private bool TryUserId(out long userId) =>
        long.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out userId);
}
