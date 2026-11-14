using CarePlus.API.DTO.MedicineDTO;
using CarePlus.API.Services.MedicineServices.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CarePlus.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class MedicineController : ControllerBase
    {
        private readonly IMedicineServices _medicineServices;

        public MedicineController(IMedicineServices medicineServices)
        {
            _medicineServices = medicineServices;
        }

        [HttpPost("Create")]
        [Authorize(Roles = "Admin,Pharmacist")]
        public async Task<IActionResult> CreateMedicine(
            MedicineRegisterRequest request)
        {
            return Ok(await _medicineServices.CreateMedicineAsync(request));
        }

        [HttpGet("Retrieve")]
        public async Task<IActionResult> RetrieveMedicines()
        {
            return Ok(await _medicineServices.RetrieveAllMedicinesAsync());
        }

        [HttpGet("RetrieveById/{medicineId}")]
        public async Task<IActionResult> RetrieveMedicineById(string medicineId)
        {
            return Ok(await _medicineServices.RetrieveMedicineByIdAsync(medicineId));
        }

        [HttpPut("Update")]
        [Authorize(Roles = "Admin,Pharmacist")]
        public async Task<IActionResult> UpdateMedicine(
            MedicineUpdateRequest request)
        {
            return Ok(await _medicineServices.UpdateMedicineAsync(request));
        }

        [HttpPost("{medicineId}/Photo")]
        [Authorize(Roles = "Admin,Pharmacist")]
        [RequestSizeLimit(5 * 1024 * 1024)]
        public async Task<IActionResult> UploadPhoto(string medicineId, IFormFile? file)
        {
            return Ok(await _medicineServices.SaveImageAsync(medicineId, file));
        }

        [AllowAnonymous]
        [HttpGet("Image/{medicineId}")]
        public async Task<IActionResult> Image(string medicineId)
        {
            var path = await _medicineServices.ResolveStoredImageAsync(medicineId);
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
