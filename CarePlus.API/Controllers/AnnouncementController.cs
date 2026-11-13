using CarePlus.API.Services.NotificationServices.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CarePlus.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[AllowAnonymous]
public class AnnouncementController : ControllerBase
{
    private readonly INotificationServices _notifications;

    public AnnouncementController(INotificationServices notifications)
    {
        _notifications = notifications;
    }

    [HttpGet]
    public async Task<IActionResult> List() => Ok(await _notifications.ListPublishedAsync());
}
