using CarePlus.API.DTO.MedicineDTO;
using CarePlus.API.Services.MedicineServices.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CarePlus.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Pharmacist")]
    public class MedicineController : ControllerBase
    {
        private readonly IMedicineServices _medicineServices;

        public MedicineController(IMedicineServices medicineServices)
        {
            _medicineServices = medicineServices;
        }

        [HttpPost("Create")]
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
        public async Task<IActionResult> UpdateMedicine(
            MedicineUpdateRequest request)
        {
            return Ok(await _medicineServices.UpdateMedicineAsync(request));
        }
    }
}
