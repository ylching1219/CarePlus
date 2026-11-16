using CarePlus.API.DTO.AnnouncementDTO;
using CarePlus.API.Services.AnnouncementServices.Interface;
using CarePlus.API.Services.NotificationServices.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CarePlus.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AnnouncementController : ControllerBase
{
    private readonly INotificationServices _notifications;
    private readonly IAnnouncementServices _announcementServices;

    public AnnouncementController(
        INotificationServices notifications,
        IAnnouncementServices announcementServices)
    {
        _notifications = notifications;
        _announcementServices = announcementServices;
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> List() => Ok(await _notifications.ListPublishedAsync());

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create([FromBody] AnnouncementCreateRequest request)
        => Ok(await _announcementServices.CreateAsync(request));
}
