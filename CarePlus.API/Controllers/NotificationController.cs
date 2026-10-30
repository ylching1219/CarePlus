using CarePlus.API.DTO;
using CarePlus.API.DTO.NotificationDTO;
using CarePlus.API.Services.NotificationServices.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CarePlus.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class NotificationController : ControllerBase
{
    private readonly INotificationServices _notifications;

    public NotificationController(INotificationServices notifications)
    {
        _notifications = notifications;
    }

    [HttpGet]
    [ResponseCache(Location = ResponseCacheLocation.None, NoStore = true)]
    public async Task<IActionResult> List()
    {
        if (!TryPatientId(out var patientId)) return Unauthorized(ApiResponse<object>.Fail("This account is not a patient account.", 403));
        return await Respond(_notifications.ListAsync(patientId));
    }

    [HttpPost("{id}/read")]
    public async Task<IActionResult> MarkRead(string id)
    {
        if (!TryPatientId(out var patientId)) return Unauthorized(ApiResponse<object>.Fail("This account is not a patient account.", 403));
        return await Respond(_notifications.MarkReadAsync(patientId, id));
    }

    [HttpPost("read")]
    public async Task<IActionResult> MarkManyRead(NotificationIdsRequest request)
    {
        if (!TryPatientId(out var patientId)) return Unauthorized(ApiResponse<object>.Fail("This account is not a patient account.", 403));
        return await Respond(_notifications.MarkManyReadAsync(patientId, request.Ids));
    }

    [HttpPost("read-all")]
    public async Task<IActionResult> MarkAllRead()
    {
        if (!TryPatientId(out var patientId)) return Unauthorized(ApiResponse<object>.Fail("This account is not a patient account.", 403));
        return await Respond(_notifications.MarkAllReadAsync(patientId));
    }

    [HttpDelete]
    public async Task<IActionResult> DeleteMany(NotificationIdsRequest request)
    {
        if (!TryPatientId(out var patientId)) return Unauthorized(ApiResponse<object>.Fail("This account is not a patient account.", 403));
        return await Respond(_notifications.DeleteManyAsync(patientId, request.Ids));
    }

    private bool TryPatientId(out string patientId)
    {
        patientId = User.FindFirstValue("patientId") ?? "";
        return !string.IsNullOrWhiteSpace(patientId);
    }

    private async Task<IActionResult> Respond<T>(Task<ApiResponse<T>> operation)
    {
        var response = await operation;
        return response.IsSuccess ? Ok(response) : StatusCode(response.RespCode, response);
    }
}
