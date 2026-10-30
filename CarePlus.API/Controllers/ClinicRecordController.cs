using CarePlus.API.DTO.ClinicRecordDTO;
using CarePlus.API.Services.ClinicRecordServices.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CarePlus.API.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/[controller]")]
    public class ClinicRecordController : ControllerBase
    {
        private readonly IClinicRecordServices _clinicRecords;

        public ClinicRecordController(IClinicRecordServices clinicRecords)
        {
            _clinicRecords = clinicRecords;
        }

        [HttpGet]
        public async Task<IActionResult> List([FromQuery] string? locale = "en")
        {
            if (!long.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
            {
                return Unauthorized(ApiResponse<List<ClinicRecordResponse>>.Fail("Invalid token.", 401));
            }

            return Ok(await _clinicRecords.ListForPatientAsync(userId, locale));
        }
    }
}
