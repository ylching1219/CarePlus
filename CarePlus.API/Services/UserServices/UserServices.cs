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

        public async Task<ApiResponse<object>> RegisterUserAsync(UserRegisterRequest request)
        {
            try
            {
                var user = await _userRepo.GetAllUsersByAsync(u => u.Email == request.Email);
                if (user != null && user.Count > 0)
                {
                    return ApiResponse<object>.Fail("User already exists", 409);
                }

                var createdUser = new User
                {
                    Username = request.Username,
                    Name = request.Name,
                    Email = request.Email,
                    PasswordHash = request.Password
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

        public async Task<ApiResponse<object>> RetreiveAllUserAsync()
        {
            try
            {
                List<User> users = await _userRepo.GetAllUsersAsync();

                if (users.Count > 0)
                {
                    var result = users.Select(u => new UserRetrieveResponse
                    {
                        userId = u.UserId,
                        username = u.Username,
                        name = u.Name!,
                        email = u.Email,
                        phone = u.Phone!,
                        status = u.Status
                    }).ToList();

                    return ApiResponse<object>.Ok(result);
                }

                return ApiResponse<object>.Fail("No user found", 404);
            }
            catch (Exception ex)
            {
                return ApiResponse<object>.Fail($"An error occurred: {ex.Message}", 500);
            }
        }

        public async Task<ApiResponse<object>> RetreiveUserByIdAsync(long id)
        {
            try
            {
                var user = await _userRepo.GetUserByIdAsync(id);

                if (user == null)
                {
                    return ApiResponse<object>.Fail(
                        $"No user found with id {id}", 404);
                }

                var result = new UserRetrieveResponse
                {
                    userId = user.UserId,
                    username = user.Username,
                    name = user.Name!,
                    email = user.Email,
                    phone = user.Phone!,
                    status = user.Status
                };

                return ApiResponse<object>.Ok(result);
            }
            catch (Exception ex)
            {
                return ApiResponse<object>.Fail( $"An error occurred: {ex.Message}", 500);
            }
        }

        public async Task<ApiResponse<object>> RetrieveAllActiveUserAsync()
        {
            try
            {
                List<User> users = await _userRepo.GetAllUsersByAsync(u => u.Status == "Active");

                if (users != null && users.Count > 0)
                {
                    var result = users.Select(u => new UserRetrieveResponse
                    {
                        userId = u.UserId,
                        username = u.Username,
                        name = u.Name!,
                        email = u.Email,
                        phone = u.Phone!,
                        status = u.Status
                    }).ToList();

                    return ApiResponse<object>.Ok(result);
                }

                return ApiResponse<object>.Fail("No active user found", 404);
            }
            catch (Exception ex)
            {
                return ApiResponse<object>.Fail($"An error occurred: {ex.Message}", 500);
            }
        }

        public async Task<ApiResponse<object>> UpdateUserAsync(UserUpdateRequest request)
        {
            try
            {
                var user = await _userRepo.GetUserByIdAsync(request.Id);
                if (user != null)
                {
                    if(request.status != "Active" && request.status != "Inactive" && request.status != "Suspended")
                    {
                        return ApiResponse<object>.Fail("Invalid status input", 400);
                    }
                    user.Name = request.name ?? user.Name;
                    user.Phone = request.phone ?? user.Phone;
                    user.Status = request.status;

                    bool success = await _userRepo.UpdateUserAsync(user);
                    if (!success)
                    {
                        return ApiResponse<object>.Fail("Failed to update user", 500);
                    }
                    return ApiResponse<object>.Ok(null);
                }

                return ApiResponse<object>.Fail("User not found", 404);
            }
            catch (Exception ex)
            {
                return ApiResponse<object>.Fail($"An error occurred: {ex.Message}", 500);
            }
        }
    }
}