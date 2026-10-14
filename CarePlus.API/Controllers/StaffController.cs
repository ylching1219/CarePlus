using CarePlus.API.DTO.StaffDTO;
using CarePlus.API.Services.StaffService.Interface;
using Microsoft.AspNetCore.Mvc;


namespace CarePlus.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class StaffController : ControllerBase
    {
        private readonly IStaffServices _staffServices;

        public StaffController(IStaffServices staffServices)
        {
            _staffServices = staffServices;
        }

        [HttpPost("Register")]
        public async Task<IActionResult> StaffRegister(StaffRegisterRequest request)
        {
            return Ok(await _staffServices.RegisterStaffAsync(request));
        }
    }
}
