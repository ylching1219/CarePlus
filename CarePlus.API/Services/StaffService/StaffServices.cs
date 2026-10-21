using CarePlus.API.Data;
using CarePlus.API.DTO.StaffDTO;
using CarePlus.API.DTO.UserDTO;
using CarePlus.API.Models.Patient;
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
                var errors = await _userService.ValidateUserAsync(request);

                if(request.Role != "Pharmacist" && request.Role !="Doctor" &&  request.Role != "Admin")
                {
                    errors.Add("Invalid staff role.");
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

                var createUser = userResponse.Data as UserRegisterResponse;
                if (createUser == null)
                {
                    return ApiResponse<object>.Fail("Invalid user response", 500);
                }

                var createStaff = new Staff
                {
                    StaffId = await _counterService.GenerateNextIdAsync("StaffId"),
                    UserId = createUser.UserId,
                    Role = request.Role,
                };

                var staffId = await _staffRepo.CreateStaffAsync(createStaff);


                return ApiResponse<object>.Ok(new StaffRegisterResponse { 
                    StaffId = staffId,
                    UserId = createUser.UserId,
                    Name = request.Name,
                });
            } 
            catch (Exception ex)
            {
                return ApiResponse<object>.Fail($"An error occurred: {ex.Message}", 500);
            }
        }

        public async Task<ApiResponse<object>> RetrieveAllStaffAsync()
        {
            try 
            {
                List<Staff> staffs = await _staffRepo.GetAllStaffsAsync();

                if (staffs.Count > 0)
                {
                    var result = staffs.Select(staff => new StaffRetrieveResponse
                    {
                        StaffId = staff.StaffId,
                        Role = staff.Role,
                        UserId = staff.UserId,
                        Username = staff.User.Username,
                        Name = staff.User.Name ?? string.Empty,
                        Email = staff.User.Email,
                        Phone = staff.User.Phone ?? string.Empty,
                        Status = staff.User.Status ?? string.Empty,
                    }).ToList();

                    return ApiResponse<object>.Ok(result);
                }

                return ApiResponse<object>.Fail("No staff found", 404);
            }
            catch(Exception ex)
            {
                return ApiResponse<object>.Fail($"An error occurred: {ex.Message}", 500);
            }
        }

        public async Task<ApiResponse<object>> RetrieveStaffByIdAsync(string id)
        {
            try
            {
                var staff = await _staffRepo.GetStaffByIdAsync(id);
                if (staff != null)
                {
                    var result = new StaffRetrieveResponse
                    {
                        StaffId = staff.StaffId,
                        Role = staff.Role,
                        UserId = staff.UserId,
                        Username = staff.User.Username,
                        Name = staff.User.Name ?? string.Empty,
                        Email = staff.User.Email,
                        Phone = staff.User.Phone ?? string.Empty,
                        Status = staff.User.Status ?? string.Empty,
                    };

                    return ApiResponse<object>.Ok(result);
                }

                return ApiResponse<object>.Fail("No staff found", 404);
            }
            catch (Exception ex)
            {
                return ApiResponse<object>.Fail($"An error occurred: {ex.Message}", 500);
            }
        }

        public async Task<ApiResponse<object>> RetrieveStaffByRoleAsync(string role)
        {
            try
            {
                List<Staff> staffs = await _staffRepo.GetAllStaffsByAsync(s => s.Role == role);

                if (staffs.Count > 0)
                {
                    var result = staffs.Select(staff => new StaffRetrieveResponse
                    {
                        StaffId = staff.StaffId,
                        Role = staff.Role,
                        UserId = staff.UserId,
                        Username = staff.User.Username,
                        Name = staff.User.Name ?? string.Empty,
                        Email = staff.User.Email,
                        Phone = staff.User.Phone ?? string.Empty,
                        Status = staff.User.Status ?? string.Empty,
                    }).ToList();

                    return ApiResponse<object>.Ok(result);
                }

                return ApiResponse<object>.Fail($"No staff found in this {role}", 404);
            }
            catch (Exception ex)
            {
                return ApiResponse<object>.Fail($"An error occurred: {ex.Message}", 500);
            }
        }

        public async Task<ApiResponse<object>> UpdateStaffAsync(StaffUpdateRequest request)
        {
            try
            {
                var staff = await _staffRepo.GetStaffByIdAsync(request.StaffId);

                if (staff == null)
                {
                    return ApiResponse<object>.Fail("Staff not found", 404);
                }

                if (!string.IsNullOrWhiteSpace(request.Name))
                {
                    staff.User.Name = request.Name.Trim();
                }

                if (!string.IsNullOrWhiteSpace(request.Phone))
                {
                    staff.User.Phone = request.Phone.Trim();
                }

                if (!string.IsNullOrWhiteSpace(request.Status))
                {
                    var status = request.Status.Trim();

                    if (status != "Active" &&
                        status != "Inactive" &&
                        status != "Suspended")
                    {
                        return ApiResponse<object>.Fail("Invalid status input", 400);
                    }

                    staff.User.Status = status;
                }

                if (!string.IsNullOrWhiteSpace(request.Role))
                {
                    var role = request.Role.Trim();

                    if (role != "Pharmacist" &&
                        role != "Doctor" &&
                        role != "Admin")
                    {
                        return ApiResponse<object>.Fail("Invalid staff role.", 400);
                    }

                    staff.Role = role;
                }

                var success = await _staffRepo.UpdateStaffAsync(staff);

                if (!success)
                {
                    return ApiResponse<object>.Fail("Failed to update staff", 500);
                }

                return ApiResponse<object>.Ok(null);
            }
            catch (Exception ex)
            {
                return ApiResponse<object>.Fail($"An error occurred: {ex.Message}", 500);
            }
        }
    }
}
