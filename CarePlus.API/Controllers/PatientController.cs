using CarePlus.API.Services.UserServices.Interface;
using Microsoft.AspNetCore.Mvc;

namespace CarePlus.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PatientController : ControllerBase
    {
        private readonly IUserServices _userServices;

        public PatientController(IUserServices userServices)
        {
            _userServices = userServices;
        }


    }
}
