using CarePlus.API.DTO.ClinicRoomDTO;
using CarePlus.API.Services.ClinicRoomServices.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CarePlus.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class ClinicRoomController : ControllerBase
{
    private readonly IClinicRoomServices _clinicRoomServices;

    public ClinicRoomController(IClinicRoomServices clinicRoomServices)
    {
        _clinicRoomServices = clinicRoomServices;
    }

    [HttpGet("Retrieve")]
    public async Task<IActionResult> Retrieve()
    {
        return Ok(await _clinicRoomServices.RetrieveAllAsync());
    }

    [HttpPost("Create")]
    public async Task<IActionResult> Create(ClinicRoomCreateRequest request)
    {
        return Ok(await _clinicRoomServices.CreateAsync(request));
    }

    [HttpPut("Update")]
    public async Task<IActionResult> Update(ClinicRoomUpdateRequest request)
    {
        return Ok(await _clinicRoomServices.UpdateAsync(request));
    }
}
