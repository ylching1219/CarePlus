using CarePlus.API.Data;
using CarePlus.API.DTO.StaffDTO;
using CarePlus.API.DTO.UserDTO;
using CarePlus.API.Models.User.Staff;
using CarePlus.API.Repository.Interface;
using CarePlus.API.Services.CounterServices.Interface;
using CarePlus.API.Services.StaffService.Interface;
using CarePlus.API.Services.UserServices.Interface;

namespace CarePlus.API.Services.StaffService
{
    public class StaffServices : IStaffServices
    {
        private readonly IStaffRepository _staffRepo;
        private readonly IUserServices _userService;
        private readonly ICounterServices _counterService;

        public StaffServices(
           IStaffRepository staffRepo,
           IUserServices userServices,
           ICounterServices counterService)
        {
            _staffRepo = staffRepo;
            _userService = userServices;
            _counterService = counterService;
        }
        public async Task<ApiResponse<object>> RegisterStaffAsync(StaffRegisterRequest request)
        {
            try 
            { 
                if(request.Role != "Pharmacist" && request.Role !="Doctor" &&  request.Role != "Admin")
                {
                    return ApiResponse<object>.Fail("Invalid Role Type", 400);
                }

                var userResponse = await _userService.RegisterUserAsync(request);

                if (!userResponse.IsSuccess)
                {
                    return ApiResponse<object>.Fail(userResponse.RespMessage, userResponse.RespCode);
                }

                var createUser = userResponse.Data as UserRegisterResponse;
                if (createUser == null)
                {
                    return ApiResponse<object>.Fail("Invalid user response", 500);
                }

                var createStaff = new Staff
                {
                    StaffId = await _counterService.GenerateNextIdAsync("StaffId"),
                    UserId = createUser.userId,
                    Role = request.Role,
                };

                var staffId = await _staffRepo.CreateStaffAsync(createStaff);


                return ApiResponse<object>.Ok(new StaffRegisterResponse { 
                    StaffId = staffId,
                    UsertId = createUser.userId,
                    Name = request.Name,
                });
            } 
            catch (Exception ex)
            {
                return ApiResponse<object>.Fail($"An error occurred: {ex.Message}", 500);
            }
        }

        public Task<ApiResponse<object>> RetreiveAllStaffAsync()
        {
            throw new NotImplementedException();
        }

        public Task<ApiResponse<object>> RetreiveStaffByIdAsync(long id)
        {
            throw new NotImplementedException();
        }

        public Task<ApiResponse<object>> RetrieveAllActiveStaffAsync()
        {
            throw new NotImplementedException();
        }

        public Task<ApiResponse<object>> UpdateStaffAsync()
        {
            throw new NotImplementedException();
        }
    }
}
