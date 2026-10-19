using CarePlus.API.DTO.UserDTO;
using CarePlus.API.Services.UserServices.Interface;
using Microsoft.AspNetCore.Mvc;
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

        [HttpPost("Login")]
        public async Task<IActionResult> UserLogin(UserLoginRequest request)
        {
            return Ok(await _userServices.UserLoginAsync(request));
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
