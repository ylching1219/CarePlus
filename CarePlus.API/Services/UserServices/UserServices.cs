using CarePlus.API.DTO;
using CarePlus.API.DTO.UserDTO;
using CarePlus.API.Models.User;
using CarePlus.API.Repository.Interface;
using CarePlus.API.Services.JWTServices.Interface;
using CarePlus.API.Services.UserServices.Interface;
using Microsoft.AspNetCore.Identity;


namespace CarePlus.API.Services.UserServices
{
    public class UserServices : IUserServices
    {
        private readonly IUserRepository _userRepo;
        private readonly PasswordHasher<User> _passwordHasher = new();
        private readonly IPatientRepository _patientRepo;
        private readonly IStaffRepository _staffRepo;
        private readonly IJWTServices _jwtService;
        public UserServices(IUserRepository userRepo,IPatientRepository patientRepo , IStaffRepository staffRepo , IJWTServices jwtService)
        {
            _userRepo = userRepo;
            _patientRepo = patientRepo;
            _staffRepo = staffRepo;
            _jwtService = jwtService;
        }

        public async Task<List<string>> ValidateUserAsync(UserRegisterRequest request)
        {
            var errors = new List<string>();

            var checkEmail = await _userRepo.GetAllUsersByAsync(u => u.Email == request.Email);
            if (checkEmail != null && checkEmail.Count > 0)
            {
                errors.Add("Email already exists");
            }

            var checkUsername = await _userRepo.GetAllUsersByAsync(u => u.Username == request.Username);
            if (checkUsername != null && checkUsername.Count > 0)
            {
                errors.Add("Username already exists");
            }

            return errors;
        }

        public async Task<ApiResponse<object>> RegisterUserAsync(UserRegisterRequest request)
        {
            try
            {
                var errors = await ValidateUserAsync( request);

                if (errors.Count > 0)
                {
                    return ApiResponse<object>.Fail("Validation failed.", 400, errors);
                }

                var createdUser = new User
                {
                    Username = request.Username,
                    Name = request.Name,
                    Email = request.Email,
                };

                createdUser.PasswordHash = HashPassword(createdUser, request.Password);
                
                long userId = await _userRepo.CreateUserAsync(createdUser);

                if (userId > 0)
                {
                    return ApiResponse<object>.Ok(new UserRegisterResponse
                    {
                        UserId = userId,
                        Name = request.Name
                    });
                }

                return ApiResponse<object>.Fail("User registration failed", 500);

            }
            catch (Exception ex)
            {
                return ApiResponse<object>.Fail($"An error occurred: {ex.Message}", 500);
            }
        }

        public async Task<ApiResponse<object>> UserLoginAsync(UserLoginRequest request)
        {
            try
            {
                var users = await _userRepo.GetAllUsersByAsync(u => u.Username == request.Identifier || u.Email == request.Identifier);      
                var user = users.FirstOrDefault();
                if (user == null)
                {
                    return ApiResponse<object>.Fail("This username or email is not registered yet.", 401);
                }

                bool passwordMatch = VerifyPassword(user,request.Password);

                if (!passwordMatch) {
                    return ApiResponse<object>.Fail("Incorrect password.", 401);
                }

                if (user.Status != "Active")
                {
                    return ApiResponse<object>.Fail("This account is not active.", 403);
                }

                var patient =await _patientRepo.GetPatientByUserIdAsync(user.UserId);

                var staff =await _staffRepo.GetStaffByUserIdAsync(user.UserId);

                if (patient == null && staff == null)
                {
                    return ApiResponse<object>.Fail(
                        "User profile was not found.", 404);
                }

                var accountType = patient != null && staff != null
                    ? "Unknown"
                    : staff != null
                        ? "Staff"
                        : "Patient";

                var role = staff?.Role ?? "Patient";

                var token = _jwtService.GenerateToken(
                    user.UserId,
                    user.Username,
                    accountType,
                    role,
                    patient?.PatientId,
                    staff?.StaffId);

                return ApiResponse<object>.Ok(new
                {
                    token
                });


            }
            catch (Exception ex)
            {
                return ApiResponse<object>.Fail($"An error occurred: {ex.Message}", 500);
            }
        }
        public async Task<ApiResponse<object>> RetrieveAllUserAsync()
        {
            try
            {
                List<User> users = await _userRepo.GetAllUsersAsync();

                if (users.Count > 0)
                {
                    var result = users.Select(u => new UserRetrieveResponse
                    {
                        UserId = u.UserId,
                        Username = u.Username,
                        Name = u.Name!,
                        Email = u.Email,
                        Phone = u.Phone!,
                        Status = u.Status
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

        public async Task<ApiResponse<object>> RetrieveUserByIdAsync(long id)
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
                    UserId = user.UserId,
                    Username = user.Username,
                    Name = user.Name!,
                    Email = user.Email,
                    Phone = user.Phone!,
                    Status = user.Status
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
                        UserId = u.UserId,
                        Username = u.Username,
                        Name = u.Name!,
                        Email = u.Email,
                        Phone = u.Phone!,
                        Status = u.Status
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
                    if(request.Status != "Active" && request.Status != "Inactive" && request.Status != "Suspended")
                    {
                        return ApiResponse<object>.Fail("Invalid status input", 400);
                    }
                    user.Name = request.Name ?? user.Name;
                    user.Phone = request.Phone ?? user.Phone;
                    user.Status = request.Status;

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

        private string HashPassword(User user, string password)
        {
            return _passwordHasher.HashPassword(user, password);
        }

        private bool VerifyPassword(User user, string password)
        {
            var result = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash!, password);
            return result == PasswordVerificationResult.Success;
        }
    }
}
