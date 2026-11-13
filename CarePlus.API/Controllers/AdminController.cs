using CarePlus.API.DTO.StaffDTO;
using CarePlus.API.Services.StaffService.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CarePlus.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")]
    public class AdminController : ControllerBase
    {
        private readonly IAdminServices _adminServices;

        public AdminController(IAdminServices adminServices)
        {
            _adminServices = adminServices;
        }

        [HttpPost("Register")]
        public async Task<IActionResult> RegisterAdmin(
            AdminRegisterRequest request)
        {
            return Ok(await _adminServices.RegisterAdminAsync(request));
        }

        [HttpGet("Retrieve")]
        public async Task<IActionResult> RetrieveAdmins()
        {
            return Ok(await _adminServices.RetrieveAllAdminsAsync());
        }

        [HttpGet("RetrieveById/{staffId}")]
        public async Task<IActionResult> RetrieveAdminById(string staffId)
        {
            return Ok(await _adminServices.RetrieveAdminByIdAsync(staffId));
        }

        [HttpPut("Update")]
        public async Task<IActionResult> UpdateAdmin(
            AdminUpdateRequest request)
        {
            return Ok(await _adminServices.UpdateAdminAsync(request));
        }
    }
}
