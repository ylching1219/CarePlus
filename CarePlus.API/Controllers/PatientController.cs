using CarePlus.API.Services.UserServices.Interface;
using Microsoft.AspNetCore.Mvc;
using CarePlus.API.DTO.PatientDTO;
using CarePlus.API.DTO.UserDTO;
using CarePlus.API.Services.PatientServices.Interface;
using CarePlus.API.Services.EmailVerificationServices.Interface;

namespace CarePlus.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PatientController : ControllerBase
    {
        private readonly IPatientServices _patientServices;
        private readonly IUserServices _userServices;
        private readonly IEmailVerificationServices _emailVerificationServices;

        public PatientController(
            IPatientServices patientServices,
            IUserServices userServices,
            IEmailVerificationServices emailVerificationServices)
        {
            _patientServices = patientServices;
            _userServices = userServices;
            _emailVerificationServices = emailVerificationServices;
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

        [HttpPost("VerifyEmail")]
        public async Task<IActionResult> VerifyEmail(EmailVerificationRequest request)
        {
            return Ok(await _emailVerificationServices.VerifyCodeAsync(request));
        }

        [HttpPost("ResendEmailVerification")]
        public async Task<IActionResult> ResendEmailVerification(EmailVerificationResendRequest request)
        {
            return Ok(await _emailVerificationServices.ResendCodeAsync(request.ChallengeId));
        }

        [HttpGet("UsernameAvailability")]
        public async Task<IActionResult> UsernameAvailability([FromQuery] string username)
        {
            return Ok(await _userServices.CheckUsernameAvailabilityAsync(username));
        }

        [HttpGet("Retrieve")]
        public async Task<IActionResult> PatientRetrieve()
        {
            return Ok(await _patientServices.RetrieveAllPatientAsync());
        }
    }
}
