using System.Security.Claims;
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
            if (!User.IsInRole("Admin"))
            {
                var staffId = User.FindFirstValue("staffId");
                if (string.IsNullOrWhiteSpace(staffId))
                {
                    return Unauthorized(ApiResponse<object>.Fail("Invalid token.", 401));
                }

                if (!string.Equals(request.StaffId, staffId, StringComparison.OrdinalIgnoreCase))
                {
                    return StatusCode(StatusCodes.Status403Forbidden,
                        ApiResponse<object>.Fail("You can only update your own doctor profile.", 403));
                }
            }

            return Ok(await _doctorServices.UpdateDoctorAsync(request));
        }

        [HttpPost("{staffId}/Photo")]
        [RequestSizeLimit(5 * 1024 * 1024)]
        public async Task<IActionResult> UploadDoctorPhoto(string staffId, IFormFile? file)
        {
            if (User.IsInRole("Doctor") && !string.Equals(
                User.FindFirstValue("staffId"),
                staffId,
                StringComparison.OrdinalIgnoreCase))
            {
                return StatusCode(StatusCodes.Status403Forbidden,
                    ApiResponse<object>.Fail("You can only update your own doctor photo.", 403));
            }

            return Ok(await _doctorServices.SaveProfilePictureAsync(staffId, file));
        }

        [AllowAnonymous]
        [HttpGet("Public/{staffId}/Photo")]
        public async Task<IActionResult> PublicPhoto(string staffId)
        {
            var path = await _doctorServices.GetPublicPhotoFileAsync(staffId);
            if (path == null)
            {
                return NotFound();
            }

            var contentType = Path.GetExtension(path).ToLowerInvariant() switch
            {
                ".png" => "image/png",
                ".jpg" or ".jpeg" => "image/jpeg",
                ".webp" => "image/webp",
                _ => "application/octet-stream",
            };
            return PhysicalFile(path, contentType);
        }
    }
}
