using CarePlus.API.DTO.UserDTO;
using CarePlus.API.Services.UserServices.Interface;
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

        public UserController(IUserServices userServices)
        {
            _userServices = userServices;
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
    }
}
