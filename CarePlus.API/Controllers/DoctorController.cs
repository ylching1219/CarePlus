using CarePlus.API.DTO.StaffDTO;
using CarePlus.API.Services.StaffService.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CarePlus.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin,Doctor")]
    public class DoctorController : ControllerBase
    {
        private readonly IDoctorServices _doctorServices;

        public DoctorController(IDoctorServices doctorServices)
        {
            _doctorServices = doctorServices;
        }

        [Authorize(Roles = "Admin")]
        [HttpPost("Register")]
        public async Task<IActionResult> RegisterDoctor(DoctorRegisterRequest request)
        {
            return Ok(await _doctorServices.RegisterDoctorAsync(request));
        }

        [HttpGet("Retrieve")]
        public async Task<IActionResult> RetrieveDoctors()
        {
            return Ok(await _doctorServices.RetrieveAllDoctorsAsync());
        }

        [AllowAnonymous]
        [HttpGet("Public")]
        public async Task<IActionResult> RetrievePublicDoctors()
        {
            return Ok(await _doctorServices.RetrievePublicDoctorsAsync());
        }

        [AllowAnonymous]
        [HttpGet("Public/{staffId}")]
        public async Task<IActionResult> RetrievePublicDoctor(string staffId)
        {
            return Ok(await _doctorServices.RetrievePublicDoctorByIdAsync(staffId));
        }

        [HttpGet("RetrieveById/{staffId}")]
        public async Task<IActionResult> RetrieveDoctorById(string staffId)
        {
            return Ok(await _doctorServices.RetrieveDoctorByIdAsync(staffId));
        }

        [HttpPut("Update")]
        public async Task<IActionResult> UpdateDoctor(DoctorUpdateRequest request)
        {
            return Ok(await _doctorServices.UpdateDoctorAsync(request));
        }
    }
}
