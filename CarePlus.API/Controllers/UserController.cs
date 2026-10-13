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

        [HttpGet("Retreive")]
        public async Task<IActionResult> UserRetrieve()
        {
            return Ok(await _userServices.RetreiveAllUserAsync());
        }

        [HttpGet("RetreiveById")]
        public async Task<IActionResult> UserRetreiveById(int userId)
        {
            return Ok(await _userServices.RetreiveUserByIdAsync(userId));
        }

        [HttpGet("RetreiveActiveUser")]
        public async Task<IActionResult> ActiveUserRetreive()
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
