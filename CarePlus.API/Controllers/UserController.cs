using CarePlus.API.DTO.DeliveryAddressDTO;
using CarePlus.API.DTO.PaymentCardDTO;
using CarePlus.API.DTO.UserDTO;
using CarePlus.API.Services.UserServices.Interface;
using CarePlus.API.Services.TwoFactorServices.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Threading.Tasks;

namespace CarePlus.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UserController : ControllerBase
    {

        private readonly IUserServices _userServices;
        private readonly ITwoFactorServices _twoFactorServices;

        public UserController(IUserServices userServices, ITwoFactorServices twoFactorServices)
        {
            _userServices = userServices;
            _twoFactorServices = twoFactorServices;
        }

        [HttpPost("Register")]
        public async Task<IActionResult> UserRegister(UserRegisterRequest request)
        {

            return Ok(await _userServices.RegisterUserAsync(request));
        }

        [HttpGet("Retrieve")]
        public async Task<IActionResult> UserRetrieve()
        {
            return Ok(await _userServices.RetrieveAllUserAsync());
        }

        [HttpGet("RetrieveById/{userId}")]
        public async Task<IActionResult> UserRetrieveById(int userId)
        {
            return Ok(await _userServices.RetrieveUserByIdAsync(userId));
        }

        [Authorize]
        [HttpGet("Me")]
        public async Task<IActionResult> CurrentUserRetrieve()
        {
            string? userIdClaim =
                User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!long.TryParse(userIdClaim, out long userId))
            {
                return Unauthorized(
                    ApiResponse<object>.Fail("Invalid token.", 401));
            }

            return Ok(
                await _userServices.RetrieveCurrentUserAsync(userId));
        }

        [Authorize]
        [HttpGet("Activity")]
        public async Task<IActionResult> LoginActivityRetrieve()
        {
            string? userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!long.TryParse(userIdClaim, out long userId))
            {
                return Unauthorized(ApiResponse<object>.Fail("Invalid token.", 401));
            }

            return Ok(await _userServices.RetrieveLoginActivityAsync(userId));
        }


        [HttpGet("RetrieveActiveUser")]
        public async Task<IActionResult> ActiveUserRetrieve()
        {
            return Ok(await _userServices.RetrieveAllActiveUserAsync());
        }

        [HttpPut("UpdateUser")]
        public async Task<IActionResult> UpdateUser(UserUpdateRequest request)
        {
            return Ok(await _userServices.UpdateUserAsync(request));
        }

        [Authorize]
        [HttpPost("Photo")]
        [RequestSizeLimit(2 * 1024 * 1024)]
        public async Task<IActionResult> PhotoUpload(IFormFile? file)
        {
            if (!TryGetUserId(out long userId))
            {
                return Unauthorized(ApiResponse<object>.Fail("Invalid token.", 401));
            }

            return Ok(await _userServices.SaveProfilePictureAsync(userId, file));
        }

        [Authorize]
        [HttpPut("Me")]
        public async Task<IActionResult> CurrentUserUpdate(ProfileUpdateRequest request)
        {
            if (!TryGetUserId(out long userId))
            {
                return Unauthorized(ApiResponse<object>.Fail("Invalid token.", 401));
            }

            return Ok(await _userServices.UpdateCurrentProfileAsync(userId, request));
        }

        [Authorize]
        [HttpPut("Password")]
        public async Task<IActionResult> PasswordChange(PasswordChangeRequest request)
        {
            if (!TryGetUserId(out long userId))
            {
                return Unauthorized(ApiResponse<object>.Fail("Invalid token.", 401));
            }

            return Ok(await _userServices.ChangePasswordAsync(userId, request));
        }

        [Authorize]
        [HttpDelete("Me")]
        public async Task<IActionResult> CurrentAccountDelete(AccountDeleteRequest request)
        {
            if (!TryGetUserId(out long userId))
            {
                return Unauthorized(ApiResponse<object>.Fail("Invalid token.", 401));
            }

            return Ok(await _userServices.DeleteCurrentAccountAsync(userId, request));
        }

        [Authorize]
        [HttpGet("DeliveryAddress")]
        public async Task<IActionResult> DeliveryAddressRetrieve()
        {
            if (!TryGetUserId(out long userId))
            {
                return Unauthorized(ApiResponse<object>.Fail("Invalid token.", 401));
            }

            return Ok(await _userServices.RetrieveDeliveryAddressesAsync(userId));
        }

        [Authorize]
        [HttpPost("DeliveryAddress")]
        public async Task<IActionResult> DeliveryAddressAdd(DeliveryAddressSaveRequest request)
        {
            if (!TryGetUserId(out long userId))
            {
                return Unauthorized(ApiResponse<object>.Fail("Invalid token.", 401));
            }

            return Ok(await _userServices.AddDeliveryAddressAsync(userId, request));
        }

        [Authorize]
        [HttpPut("DeliveryAddress/{addressId:long}")]
        public async Task<IActionResult> DeliveryAddressUpdate(long addressId, DeliveryAddressSaveRequest request)
        {
            if (!TryGetUserId(out long userId))
            {
                return Unauthorized(ApiResponse<object>.Fail("Invalid token.", 401));
            }

            return Ok(await _userServices.UpdateDeliveryAddressAsync(userId, addressId, request));
        }

        [Authorize]
        [HttpDelete("DeliveryAddress/{addressId:long}")]
        public async Task<IActionResult> DeliveryAddressDelete(long addressId)
        {
            if (!TryGetUserId(out long userId))
            {
                return Unauthorized(ApiResponse<object>.Fail("Invalid token.", 401));
            }

            return Ok(await _userServices.DeleteDeliveryAddressAsync(userId, addressId));
        }

        [Authorize]
        [HttpPut("DeliveryAddress/Default")]
        public async Task<IActionResult> DeliveryAddressDefault(DeliveryAddressDefaultRequest request)
        {
            if (!TryGetUserId(out long userId))
            {
                return Unauthorized(ApiResponse<object>.Fail("Invalid token.", 401));
            }

            return Ok(await _userServices.SetDefaultDeliveryAddressAsync(userId, request.Id));
        }

        [Authorize]
        [HttpGet("AppSetting")]
        public async Task<IActionResult> AppSettingRetrieve()
        {
            if (!TryGetUserId(out long userId))
            {
                return Unauthorized(ApiResponse<object>.Fail("Invalid token.", 401));
            }

            return Ok(await _userServices.RetrieveAppSettingAsync(userId));
        }

        [Authorize]
        [HttpPut("AppSetting")]
        public async Task<IActionResult> AppSettingSave(AppSettingSaveRequest request)
        {
            if (!TryGetUserId(out long userId))
            {
                return Unauthorized(ApiResponse<object>.Fail("Invalid token.", 401));
            }

            return Ok(await _userServices.SaveAppSettingAsync(userId, request));
        }

        [Authorize]
        [HttpPost("TwoFactor/SendCode")]
        public async Task<IActionResult> TwoFactorCodeSend(TwoFactorSendCodeRequest request)
        {
            if (!TryGetUserId(out long userId))
            {
                return Unauthorized(ApiResponse<object>.Fail("Invalid token.", 401));
            }

            return Ok(await _twoFactorServices.SendCodeAsync(userId, request));
        }

        [Authorize]
        [HttpPost("TwoFactor/Verify")]
        public async Task<IActionResult> TwoFactorCodeVerify(TwoFactorVerifyRequest request)
        {
            if (!TryGetUserId(out long userId))
            {
                return Unauthorized(ApiResponse<object>.Fail("Invalid token.", 401));
            }

            return Ok(await _twoFactorServices.VerifyCodeAsync(userId, request));
        }

        [Authorize]
        [HttpGet("PaymentCard")]
        public async Task<IActionResult> PaymentCardRetrieve()
        {
            if (!TryGetUserId(out long userId))
            {
                return Unauthorized(ApiResponse<object>.Fail("Invalid token.", 401));
            }

            return Ok(await _userServices.RetrievePaymentCardsAsync(userId));
        }

        [Authorize]
        [HttpPost("PaymentCard")]
        public async Task<IActionResult> PaymentCardAdd(PaymentCardSaveRequest request)
        {
            if (!TryGetUserId(out long userId))
            {
                return Unauthorized(ApiResponse<object>.Fail("Invalid token.", 401));
            }

            return Ok(await _userServices.AddPaymentCardAsync(userId, request));
        }

        [Authorize]
        [HttpPut("PaymentCard/{id:long}")]
        public async Task<IActionResult> PaymentCardUpdate(long id, PaymentCardSaveRequest request)
        {
            if (!TryGetUserId(out long userId))
            {
                return Unauthorized(ApiResponse<object>.Fail("Invalid token.", 401));
            }

            return Ok(await _userServices.UpdatePaymentCardAsync(userId, id, request));
        }

        [Authorize]
        [HttpDelete("PaymentCard/{id:long}")]
        public async Task<IActionResult> PaymentCardDelete(long id)
        {
            if (!TryGetUserId(out long userId))
            {
                return Unauthorized(ApiResponse<object>.Fail("Invalid token.", 401));
            }

            return Ok(await _userServices.DeletePaymentCardAsync(userId, id));
        }

        [Authorize]
        [HttpPut("PaymentCard/Default")]
        public async Task<IActionResult> PaymentCardDefault(PaymentCardDefaultRequest request)
        {
            if (!TryGetUserId(out long userId))
            {
                return Unauthorized(ApiResponse<object>.Fail("Invalid token.", 401));
            }

            return Ok(await _userServices.SetDefaultPaymentCardAsync(userId, request.Id));
        }

        private bool TryGetUserId(out long userId)
        {
            return long.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out userId);
        }
    }
}
