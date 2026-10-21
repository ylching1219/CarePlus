using CarePlus.API.Services.UserServices.Interface;
using Microsoft.AspNetCore.Mvc;
using CarePlus.API.DTO.PatientDTO;
using CarePlus.API.DTO.UserDTO;
using CarePlus.API.Services.PatientServices.Interface;

namespace CarePlus.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PatientController : ControllerBase
    {
        private readonly IPatientServices _patientServices;
        private readonly IUserServices _userServices;

        public PatientController(IPatientServices patientServices, IUserServices userServices)
        {
            _patientServices = patientServices;
            _userServices = userServices;
        }

        [HttpPost("Login")]
        public async Task<IActionResult> PatientLogin(UserLoginRequest request)
        {
            return Ok(await _userServices.PatientLoginAsync(request));
        }

        [HttpPost("Register")]
        public async Task<IActionResult> PatientRegister(PatientRegisterRequest request)
        {
            return Ok(await _patientServices.RegisterPatientAsync(request));
        }

        [HttpGet("Retrieve")]
        public async Task<IActionResult> PatientRetrieve()
        {
            return Ok(await _patientServices.RetrieveAllPatientAsync());
        }
    }
}
