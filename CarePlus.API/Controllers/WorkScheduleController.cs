using CarePlus.API.DTO.WorkScheduleDTO;
using CarePlus.API.Services.WorkScheduleServices.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CarePlus.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin,Doctor,Pharmacist")]
    public class WorkScheduleController : ControllerBase
    {
        private readonly IWorkScheduleServices _workScheduleServices;

        public WorkScheduleController(IWorkScheduleServices workScheduleServices)
        {
            _workScheduleServices = workScheduleServices;
        }

        [HttpPost("Create")]
        public async Task<IActionResult> CreateWorkSchedule(
            WorkScheduleCreateRequest request)
        {
            return Ok(await _workScheduleServices
                .CreateWorkScheduleAsync(request));
        }

        [HttpGet("Retrieve")]
        public async Task<IActionResult> RetrieveWorkSchedules()
        {
            return Ok(await _workScheduleServices
                .RetrieveAllWorkSchedulesAsync());
        }

        [HttpGet("RetrieveById/{scheduleId}")]
        public async Task<IActionResult> RetrieveWorkScheduleById(string scheduleId)
        {
            return Ok(await _workScheduleServices
                .RetrieveWorkScheduleByIdAsync(scheduleId));
        }

        [HttpGet("RetrieveByStaffId/{staffId}")]
        public async Task<IActionResult> RetrieveWorkSchedulesByStaffId(string staffId)
        {
            return Ok(await _workScheduleServices
                .RetrieveWorkSchedulesByStaffIdAsync(staffId));
        }

        [HttpPut("Update")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateWorkSchedule(
            WorkScheduleUpdateRequest request)
        {
            return Ok(await _workScheduleServices
                .UpdateWorkScheduleAsync(request));
        }
    }
}
