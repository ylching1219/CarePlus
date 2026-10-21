using CarePlus.API.DTO.StaffDTO;
using CarePlus.API.Services.StaffService.Interface;
using CarePlus.API.DTO.UserDTO;
using CarePlus.API.Services.UserServices.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;


namespace CarePlus.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin,Doctor,Pharmacist")]
    public class StaffController : ControllerBase
    {
        private readonly IStaffServices _staffServices;
        private readonly IUserServices _userServices;

        public StaffController(IStaffServices staffServices, IUserServices userServices)
        {
            _staffServices = staffServices;
            _userServices = userServices;
        }

        [AllowAnonymous]
        [HttpPost("Login")]
        public async Task<IActionResult> StaffLogin(UserLoginRequest request)
        {
            return Ok(await _userServices.StaffLoginAsync(request));
        }

        [Authorize(Roles = "Admin")]
        [HttpPost("Register")]
        public async Task<IActionResult> StaffRegister(StaffRegisterRequest request)
        {
            return Ok(await _staffServices.RegisterStaffAsync(request));
        }

        [HttpGet("Retrieve")]
        public async Task<IActionResult> StaffRetrieve()
        {
            return Ok(await _staffServices.RetrieveAllStaffAsync());
        }

        [HttpGet("RetrieveById/{staffId}")]
        public async Task<IActionResult> StaffRetrieveById(string staffId)
        {
            return Ok(await _staffServices.RetrieveStaffByIdAsync(staffId));
        }

        [HttpGet("RetrieveByRole/{role}")]
        public async Task<IActionResult> StaffRetrieveByRole(string role)
        {
            return Ok(await _staffServices.RetrieveStaffByRoleAsync(role));
        }

        [HttpPut("UpdateStaff")]
        public async Task<IActionResult> UpdateStaff(StaffUpdateRequest request)
        {
            return Ok(await _staffServices.UpdateStaffAsync(request));
        }
    }
}
