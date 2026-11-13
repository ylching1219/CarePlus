using CarePlus.API.Services.UserServices.Interface;
using Microsoft.AspNetCore.Mvc;
using CarePlus.API.DTO.PatientDTO;
using CarePlus.API.DTO.UserDTO;
using CarePlus.API.Services.PatientServices.Interface;
using CarePlus.API.Services.EmailVerificationServices.Interface;
using CarePlus.API.Services.PasswordResetServices.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using CarePlus.API.Services.RegistrationEmailVerificationServices.Interface;

namespace CarePlus.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PatientController : ControllerBase
    {
        private readonly IPatientServices _patientServices;
        private readonly IUserServices _userServices;
        private readonly IEmailVerificationServices _emailVerificationServices;
        private readonly IPasswordResetServices _passwordResetServices;
        private readonly IRegistrationEmailVerificationServices _registrationEmailVerificationServices;

        public PatientController(
            IPatientServices patientServices,
            IUserServices userServices,
            IEmailVerificationServices emailVerificationServices,
            IPasswordResetServices passwordResetServices,
            IRegistrationEmailVerificationServices registrationEmailVerificationServices)
        {
            _patientServices = patientServices;
            _userServices = userServices;
            _emailVerificationServices = emailVerificationServices;
            _passwordResetServices = passwordResetServices;
            _registrationEmailVerificationServices = registrationEmailVerificationServices;
        }

        [AllowAnonymous]
        [HttpPost("Login")]
        public async Task<IActionResult> PatientLogin(UserLoginRequest request)
        {
            return Ok(await _userServices.PatientLoginAsync(request));
        }

        [HttpPost("Login/VerifyTwoFactor")]
        [AllowAnonymous]
        [EnableRateLimiting("email-code")]
        public async Task<IActionResult> PatientLoginVerifyTwoFactor(LoginTwoFactorVerifyRequest request)
        {
            return Ok(await _userServices.CompletePatientLoginTwoFactorAsync(request));
        }

        [HttpPost("Login/ResendTwoFactor")]
        [AllowAnonymous]
        [EnableRateLimiting("email-code")]
        public async Task<IActionResult> PatientLoginResendTwoFactor(LoginTwoFactorResendRequest request)
        {
            return Ok(await _userServices.ResendPatientLoginTwoFactorAsync(request));
        }

        [AllowAnonymous]
        [HttpPost("Register")]
        public async Task<IActionResult> PatientRegister(PatientRegisterRequest request)
        {
            return Ok(await _patientServices.RegisterPatientAsync(request));
        }

        [AllowAnonymous]
        [HttpPost("VerifyEmail")]
        public async Task<IActionResult> VerifyEmail(EmailVerificationRequest request)
        {
            return Ok(await _emailVerificationServices.VerifyCodeAsync(request));
        }

        [AllowAnonymous]
        [HttpPost("ResendEmailVerification")]
        public async Task<IActionResult> ResendEmailVerification(EmailVerificationResendRequest request)
        {
            return Ok(await _emailVerificationServices.ResendCodeAsync(request.ChallengeId));
        }

        [AllowAnonymous]
        [HttpGet("UsernameAvailability")]
        public async Task<IActionResult> UsernameAvailability([FromQuery] string username)
        {
            return Ok(await _userServices.CheckUsernameAvailabilityAsync(username));
        }

        [HttpPost("RegistrationEmail/Request")]
        [AllowAnonymous]
        [EnableRateLimiting("email-code")]
        public async Task<IActionResult> RequestRegistrationEmailCode(
            RegistrationEmailCodeRequest request,
            CancellationToken cancellationToken)
        {
            return Ok(await _registrationEmailVerificationServices.RequestCodeAsync(request, cancellationToken));
        }

        [HttpPost("RegistrationEmail/Verify")]
        [AllowAnonymous]
        [EnableRateLimiting("email-code")]
        public async Task<IActionResult> VerifyRegistrationEmailCode(
            RegistrationEmailCodeVerifyRequest request,
            CancellationToken cancellationToken)
        {
            return Ok(await _registrationEmailVerificationServices.VerifyCodeAsync(request, cancellationToken));
        }

        [HttpPost("ForgotPassword/Request")]
        [AllowAnonymous]
        [EnableRateLimiting("password-reset")]
        public async Task<IActionResult> RequestPasswordReset(
            PasswordResetRequestCodeRequest request,
            CancellationToken cancellationToken)
        {
            return Ok(await _passwordResetServices.RequestCodeAsync(request, cancellationToken));
        }

        [HttpPost("ForgotPassword/Verify")]
        [AllowAnonymous]
        [EnableRateLimiting("password-reset")]
        public async Task<IActionResult> VerifyPasswordResetCode(
            PasswordResetVerifyCodeRequest request,
            CancellationToken cancellationToken)
        {
            return Ok(await _passwordResetServices.VerifyCodeAsync(request, cancellationToken));
        }

        [HttpPost("ForgotPassword/Reset")]
        [AllowAnonymous]
        [EnableRateLimiting("password-reset")]
        public async Task<IActionResult> CompletePasswordReset(
            PasswordResetCompleteRequest request,
            CancellationToken cancellationToken)
        {
            return Ok(await _passwordResetServices.ResetPasswordAsync(request, cancellationToken));
        }

        [Authorize(Roles = "Admin,Doctor,Pharmacist")]
        [HttpGet("Retrieve")]
        public async Task<IActionResult> PatientRetrieve()
        {
            return Ok(await _patientServices.RetrieveAllPatientAsync());
        }
    }
}
