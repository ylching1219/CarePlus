using CarePlus.API.Services.AnnouncementServices.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CarePlus.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[AllowAnonymous]
public class AnnouncementController : ControllerBase
{
    private readonly IAnnouncementServices _announcements;

    public AnnouncementController(IAnnouncementServices announcements)
    {
        _announcements = announcements;
    }

    [HttpGet]
    public async Task<IActionResult> List() => Ok(await _announcements.ListPublishedAsync());
}
