using CarePlus.API.DTO.MedicineBatchDTO;
using CarePlus.API.Services.MedicineBatchServices.Interface;
using Microsoft.AspNetCore.Mvc;

namespace CarePlus.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MedicineBatchController : ControllerBase
    {
        private readonly IMedicineBatchServices _medicineBatchServices;

        public MedicineBatchController(
            IMedicineBatchServices medicineBatchServices)
        {
            _medicineBatchServices = medicineBatchServices;
        }

        [HttpPost("Create")]
        public async Task<IActionResult> CreateMedicineBatch(
            MedicineBatchRegisterRequest request)
        {
            return Ok(await _medicineBatchServices
                .CreateMedicineBatchAsync(request));
        }

        [HttpGet("Retrieve")]
        public async Task<IActionResult> RetrieveMedicineBatches()
        {
            return Ok(await _medicineBatchServices
                .RetrieveAllMedicineBatchesAsync());
        }

        [HttpGet("RetrieveById/{batchId}")]
        public async Task<IActionResult> RetrieveMedicineBatchById(
            string batchId)
        {
            return Ok(await _medicineBatchServices
                .RetrieveMedicineBatchByIdAsync(batchId));
        }

        [HttpPut("Update")]
        public async Task<IActionResult> UpdateMedicineBatch(
            MedicineBatchUpdateRequest request)
        {
            return Ok(await _medicineBatchServices
                .UpdateMedicineBatchAsync(request));
        }
    }
}
