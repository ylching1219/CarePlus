using CarePlus.API.DTO.MedicalRecordDTO;
using CarePlus.API.Services.MedicalRecordServices.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CarePlus.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Doctor")]
public class MedicalRecordController : ControllerBase
{
    private readonly IMedicalRecordServices _medicalRecords;

    public MedicalRecordController(IMedicalRecordServices medicalRecords)
    {
        _medicalRecords = medicalRecords;
    }

    [HttpGet("Retrieve")]
    public async Task<IActionResult> Retrieve()
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized(ApiResponse<List<MedicalRecordResponse>>.Fail("Invalid token.", 401));
        }

        return Ok(await _medicalRecords.RetrieveForDoctorAsync(userId));
    }

    [HttpPost("Create")]
    public async Task<IActionResult> Create(MedicalRecordCreateRequest request)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized(ApiResponse<MedicalRecordResponse>.Fail("Invalid token.", 401));
        }

        return Ok(await _medicalRecords.CreateAsync(userId, request));
    }

    [HttpPut("Update")]
    public async Task<IActionResult> Update(MedicalRecordUpdateRequest request)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized(ApiResponse<MedicalRecordResponse>.Fail("Invalid token.", 401));
        }

        return Ok(await _medicalRecords.UpdateAsync(userId, request));
    }

    private bool TryGetUserId(out long userId) =>
        long.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out userId);
}
