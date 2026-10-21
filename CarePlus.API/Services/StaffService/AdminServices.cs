using CarePlus.API.DTO.StaffDTO;
using CarePlus.API.DTO.UserDTO;
using CarePlus.API.Models.User.Staff.Admin;
using CarePlus.API.Repository.Interface;
using CarePlus.API.Services.CounterServices.Interface;
using CarePlus.API.Services.StaffService.Interface;
using CarePlus.API.Services.UserServices.Interface;

namespace CarePlus.API.Services.StaffService
{
    public class AdminServices : IAdminServices
    {
        private readonly IAdminRepository _adminRepo;
        private readonly IUserServices _userService;
        private readonly ICounterServices _counterService;

        public AdminServices(
            IAdminRepository adminRepo,
            IUserServices userService,
            ICounterServices counterService)
        {
            _adminRepo = adminRepo;
            _userService = userService;
            _counterService = counterService;
        }

        public async Task<ApiResponse<object>> RegisterAdminAsync(
            AdminRegisterRequest request)
        {
            try
            {
                
                var errors = await _userService.ValidateUserAsync(request);

                if (request.AccessLevel < 1 || request.AccessLevel > 3)
                {
                    errors.Add("Access level must be between 1 and 3");
                }

                var departments = new[]
                {
                    "Human Resources",
                    "Clinic Operations",
                    "IT",
                    "Administration"
                };

                if (request.Department == null ||!departments.Contains(request.Department))
                {
                    errors.Add("Invalid department");
                }

                if (errors.Count > 0)
                {
                    return ApiResponse<object>.Fail("Validation failed.", 400, errors);
                }

                var userResponse = await _userService.RegisterUserAsync(request);

                if (!userResponse.IsSuccess)
                {
                    return userResponse;
                }

                var user = userResponse.Data as UserRegisterResponse;

                if (user == null)
                {
                    return ApiResponse<object>.Fail("Invalid user response", 500);
                }

                var admin = new Admin
                {
                    StaffId = await _counterService.GenerateNextIdAsync("StaffId"),
                    UserId = user.UserId,
                    Role = "Admin",
                    AccessLevel = request.AccessLevel,
                    Department = request.Department
                };

                var staffId = await _adminRepo.CreateAdminAsync(admin);

                return ApiResponse<object>.Ok(new AdminRegisterResponse
                {
                    StaffId = staffId,
                    UserId = user.UserId,
                    Name = request.Name,
                    AccessLevel = admin.AccessLevel,
                    Department = admin.Department
                });
            }
            catch (Exception ex)
            {
                return ApiResponse<object>.Fail($"An error occurred: {ex.Message}", 500);
            }
        }

        public async Task<ApiResponse<object>> RetrieveAllAdminsAsync()
        {
            try
            {
                var admins = await _adminRepo.GetAllAdminsAsync();

                if (admins.Count == 0)
                {
                    return ApiResponse<object>.Fail("No admin found", 404);
                }

                var result = admins.Select(admin => new AdminRetrieveResponse
                {
                    StaffId = admin.StaffId,
                    UserId = admin.UserId,
                    Username = admin.User.Username,
                    Name = admin.User.Name ?? string.Empty,
                    Email = admin.User.Email,
                    Phone = admin.User.Phone ?? string.Empty,
                    Status = admin.User.Status,
                    Role = admin.Role,
                    AccessLevel = admin.AccessLevel,
                    Department = admin.Department
                }).ToList();

                return ApiResponse<object>.Ok(result);
            }
            catch (Exception ex)
            {
                return ApiResponse<object>.Fail(
                    $"An error occurred: {ex.Message}", 500);
            }
        }

        public async Task<ApiResponse<object>> RetrieveAdminByIdAsync(
            string staffId)
        {
            try
            {
                var admin = await _adminRepo.GetAdminByIdAsync(staffId);

                if (admin == null)
                {
                    return ApiResponse<object>.Fail("Admin not found", 404);
                }

                return ApiResponse<object>.Ok(new AdminRetrieveResponse
                {
                    StaffId = admin.StaffId,
                    UserId = admin.UserId,
                    Username = admin.User.Username,
                    Name = admin.User.Name ?? string.Empty,
                    Email = admin.User.Email,
                    Phone = admin.User.Phone ?? string.Empty,
                    Status = admin.User.Status,
                    Role = admin.Role,
                    AccessLevel = admin.AccessLevel,
                    Department = admin.Department
                });
            }
            catch (Exception ex)
            {
                return ApiResponse<object>.Fail(
                    $"An error occurred: {ex.Message}", 500);
            }
        }

        public async Task<ApiResponse<object>> UpdateAdminAsync(
            AdminUpdateRequest request)
        {
            try
            {
                var admin = await _adminRepo.GetAdminByIdAsync(request.StaffId);

                if (admin == null)
                {
                    return ApiResponse<object>.Fail("Admin not found", 404);
                }

                if (request.AccessLevel != null)
                {
                    if (request.AccessLevel < 1 || request.AccessLevel > 3)
                    {
                        return ApiResponse<object>.Fail(
                            "Access level must be between 1 and 3", 400);
                    }

                    admin.AccessLevel = request.AccessLevel.Value;
                }

                if (!string.IsNullOrWhiteSpace(request.Department))
                {
                    admin.Department = request.Department.Trim();
                }

                var success = await _adminRepo.UpdateAdminAsync(admin);

                if (!success)
                {
                    return ApiResponse<object>.Fail(
                        "Failed to update admin", 500);
                }

                return ApiResponse<object>.Ok(null);
            }
            catch (Exception ex)
            {
                return ApiResponse<object>.Fail(
                    $"An error occurred: {ex.Message}", 500);
            }
        }

    }
}
