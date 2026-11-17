using System.Security.Claims;
using CarePlus.API.DTO.PrescriptionDTO;
using CarePlus.API.Extension;
using CarePlus.API.Services.PrescriptionServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CarePlus.API.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class PrescriptionController(PrescriptionServices prescriptions) : ControllerBase
{
    [Authorize(Roles = "Admin,Doctor,Pharmacist")]
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

    [Authorize(Roles = "Admin,Doctor,Pharmacist")]
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

    [Authorize(Roles = "Admin,Pharmacist")]
    [HttpPatch("{prescriptionId}/Complete")]
    public async Task<IActionResult> CompleteDispensing(string prescriptionId, CancellationToken cancellationToken)
    {
        if (!TryUserId(out var userId))
        {
            return Unauthorized(ApiResponse<PrescriptionSummaryResponse>.Fail("Invalid token.", 401));
        }

        return this.ApiResult(await prescriptions.CompleteDispensingAsync(userId, prescriptionId, cancellationToken));
    }

    [Authorize(Roles = "Admin,Pharmacist")]
    [HttpPatch("{prescriptionId}/Preparing")]
    public async Task<IActionResult> MarkPreparing(string prescriptionId, CancellationToken cancellationToken)
    {
        if (!TryUserId(out var userId))
        {
            return Unauthorized(ApiResponse<PrescriptionSummaryResponse>.Fail("Invalid token.", 401));
        }

        return this.ApiResult(await prescriptions.MarkPreparingAsync(userId, prescriptionId, cancellationToken));
    }

    [Authorize(Roles = "Admin,Pharmacist")]
    [HttpPatch("{prescriptionId}/ReadyForPickup")]
    public async Task<IActionResult> MarkReadyForPickup(string prescriptionId, CancellationToken cancellationToken)
    {
        if (!TryUserId(out var userId))
        {
            return Unauthorized(ApiResponse<PrescriptionSummaryResponse>.Fail("Invalid token.", 401));
        }

        return this.ApiResult(await prescriptions.MarkReadyForPickupAsync(userId, prescriptionId, cancellationToken));
    }

    [HttpGet("Dispensed")]
    public async Task<IActionResult> Dispensed(CancellationToken cancellationToken)
    {
        if (!TryUserId(out var userId))
        {
            return Unauthorized(ApiResponse<List<DispensedPrescriptionResponse>>.Fail("Invalid token.", 401));
        }

        var rows = await prescriptions.ListDispensedForPatientAsync(userId, cancellationToken);
        return Ok(ApiResponse<List<DispensedPrescriptionResponse>>.Ok(rows));
    }

    private bool TryUserId(out long userId) =>
        long.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out userId);
}
