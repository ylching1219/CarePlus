using CarePlus.API.DTO;
using CarePlus.API.DTO.FamilyDTO;
using CarePlus.API.Services.FamilyServices.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CarePlus.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class FamilyController : ControllerBase
{
    private readonly IFamilyServices _familyServices;

    public FamilyController(IFamilyServices familyServices)
    {
        _familyServices = familyServices;
    }

    [HttpGet]
    [ResponseCache(Location = ResponseCacheLocation.None, NoStore = true)]
    public async Task<IActionResult> GetOverview()
    {
        return await Execute((patientId) => _familyServices.GetOverviewAsync(patientId));
    }

    [HttpPost]
    public async Task<IActionResult> Create(FamilyCreateRequest request) => await Execute((patientId) => _familyServices.CreateAsync(patientId, request));

    [HttpPut]
    public async Task<IActionResult> Rename(FamilyRenameRequest request) => await Execute((patientId) => _familyServices.RenameAsync(patientId, request));

    [HttpPost("members")]
    public async Task<IActionResult> Invite(FamilyInviteRequest request) => await Execute((patientId) => _familyServices.InviteAsync(patientId, request));

    [HttpPost("invites/{familyMemberId:long}/accept")]
    public async Task<IActionResult> AcceptInvite(long familyMemberId) => await Execute((patientId) => _familyServices.AcceptInviteAsync(patientId, familyMemberId));

    [HttpPost("invites/{familyMemberId:long}/decline")]
    public async Task<IActionResult> DeclineInvite(long familyMemberId) => await Execute((patientId) => _familyServices.DeclineInviteAsync(patientId, familyMemberId));

    [HttpDelete("invites/{familyMemberId:long}")]
    public async Task<IActionResult> CancelInvite(long familyMemberId) => await Execute((patientId) => _familyServices.CancelInviteAsync(patientId, familyMemberId));

    [HttpPut("members/{familyMemberId:long}/access")]
    public async Task<IActionResult> UpdateAccess(long familyMemberId, FamilyAccessUpdateRequest request) => await Execute((patientId) => _familyServices.UpdateAccessAsync(patientId, familyMemberId, request));

    [HttpPut("members/me/display-name")]
    public async Task<IActionResult> RenameSelf(FamilyMemberDisplayNameRequest request) => await Execute((patientId) => _familyServices.RenameSelfAsync(patientId, request));

    [HttpDelete("members/{familyMemberId:long}")]
    public async Task<IActionResult> RemoveMember(long familyMemberId) => await Execute((patientId) => _familyServices.RemoveMemberAsync(patientId, familyMemberId));

    [HttpPost("leave")]
    public async Task<IActionResult> Leave(FamilyLeaveRequest request) => await Execute((patientId) => _familyServices.LeaveAsync(patientId, request));

    [HttpDelete]
    public async Task<IActionResult> Delete() => await Execute((patientId) => _familyServices.DeleteAsync(patientId));

    private async Task<IActionResult> Execute(Func<string, Task<ApiResponse<FamilyOverviewResponse>>> action)
    {
        var patientId = User.FindFirstValue("patientId");
        if (string.IsNullOrWhiteSpace(patientId)) return Unauthorized(ApiResponse<object>.Fail("This account is not a patient account.", 403));
        var response = await action(patientId);
        return response.IsSuccess ? Ok(response) : StatusCode(response.RespCode, response);
    }
}
