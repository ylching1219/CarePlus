using CarePlus.API.DTO.StaffDTO;
using CarePlus.API.Services.StaffService.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CarePlus.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin,Pharmacist")]
    public class PharmacistController : ControllerBase
    {
        private readonly IPharmacistServices _pharmacistServices;

        public PharmacistController(IPharmacistServices pharmacistServices)
        {
            _pharmacistServices = pharmacistServices;
        }

        [Authorize(Roles = "Admin")]
        [HttpPost("Register")]
        public async Task<IActionResult> RegisterPharmacist(
            PharmacistRegisterRequest request)
        {
            return Ok(await _pharmacistServices.RegisterPharmacistAsync(request));
        }

        [HttpGet("Retrieve")]
        public async Task<IActionResult> RetrievePharmacists()
        {
            return Ok(await _pharmacistServices.RetrieveAllPharmacistsAsync());
        }

        [HttpGet("RetrieveById/{staffId}")]
        public async Task<IActionResult> RetrievePharmacistById(string staffId)
        {
            return Ok(await _pharmacistServices.RetrievePharmacistByIdAsync(staffId));
        }

        [HttpPut("Update")]
        public async Task<IActionResult> UpdatePharmacist(
            PharmacistUpdateRequest request)
        {
            return Ok(await _pharmacistServices.UpdatePharmacistAsync(request));
        }
    }
}
