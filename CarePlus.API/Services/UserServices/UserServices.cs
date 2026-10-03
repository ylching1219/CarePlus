using CarePlus.API.DTO;
using CarePlus.API.DTO.UserDTO;
using CarePlus.API.Models.User;
using CarePlus.API.Repository.Interface;
using CarePlus.API.Services.UserServices.Interface;
using Microsoft.AspNetCore.Http.HttpResults;

namespace CarePlus.API.Services.UserServices
{
    public class UserServices : IUserServices
    {
        private readonly IUserRepository _userRepo;

        public UserServices(IUserRepository userRepo)
        {
            _userRepo = userRepo;
        }

        public  async Task<ApiResponse<Object>> RegisterUserAsync(UserRegisterRequest request)
        {
            try
            {
                var user = await _userRepo.GetAllUsersByAsync(u => u.Email == request.Email);
                if (user != null && user.Count > 0)
                {
                    return ApiResponse<Object>.Fail("User already exists", 409);
                }

                var createdUser = new User
                {
                    Username = request.Username,
                    Name = request.Name,
                    Email = request.Email,
                    Password = request.Password
                };

                long userId = await _userRepo.CreateUserAsync(createdUser);

                if (userId > 0)
                {
                    return ApiResponse<object>.Ok(new UserRegisterResponse
                    {
                        userId = userId,
                        name = request.Name
                    });
                }

                return ApiResponse<Object>.Fail("User registration failed", 500);

            }
            catch (Exception ex)
            {
                return ApiResponse<Object>.Fail($"An error occurred: {ex.Message}", 500);
            }
        }
    }
}
