using CarePlus.API.DTO.StaffDTO;
using CarePlus.API.Services.StaffService.Interface;
using Microsoft.AspNetCore.Mvc;

namespace CarePlus.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DoctorController : ControllerBase
    {
        private readonly IDoctorServices _doctorServices;

        public DoctorController(IDoctorServices doctorServices)
        {
            _doctorServices = doctorServices;
        }

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
