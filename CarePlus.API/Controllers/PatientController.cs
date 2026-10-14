using CarePlus.API.Services.UserServices.Interface;
using Microsoft.AspNetCore.Mvc;
using CarePlus.API.DTO.PatientDTO;
using CarePlus.API.Services.PatientServices.Interface;

namespace CarePlus.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PatientController : ControllerBase
    {
        private readonly IPatientServices _patientServices;

        public PatientController(IPatientServices patientServices)
        {
            _patientServices = patientServices;
        }

        [HttpPost("Register")]
        public async Task<IActionResult> PatientRegister(PatientRegisterRequest request)
        {
            return Ok(await _patientServices.RegisterPatientAsync(request));
        }
    }
}
