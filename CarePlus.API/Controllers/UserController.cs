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
    }
}
