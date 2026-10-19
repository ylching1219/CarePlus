using CarePlus.API.DTO.StaffDTO;
using CarePlus.API.DTO.UserDTO;
using CarePlus.API.Models.User.Staff.Pharmacist;
using CarePlus.API.Repository.Interface;
using CarePlus.API.Services.CounterServices.Interface;
using CarePlus.API.Services.StaffService.Interface;
using CarePlus.API.Services.UserServices.Interface;

namespace CarePlus.API.Services.StaffService
{
    public class PharmacistServices : IPharmacistServices
    {
        private readonly IPharmacistRepository _pharmacistRepo;
        private readonly IUserServices _userService;
        private readonly ICounterServices _counterService;

        public PharmacistServices(
            IPharmacistRepository pharmacistRepo,
            IUserServices userService,
            ICounterServices counterService)
        {
            _pharmacistRepo = pharmacistRepo;
            _userService = userService;
            _counterService = counterService;
        }

        public async Task<ApiResponse<object>> RegisterPharmacistAsync(
            PharmacistRegisterRequest request)
        {
            try
            {
                var errors = await _userService.ValidateUserAsync(request);

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

                var pharmacist = new Pharmacist
                {
                    StaffId = await _counterService.GenerateNextIdAsync("StaffId"),
                    UserId = user.UserId,
                    Role = "Pharmacist",
                    LicenseNumber = request.LicenseNumber,
                    Qualification = request.Qualification,
                    Certification = request.Certification,
                    YearsOfExperience = request.YearsOfExperience
                };

                var staffId = await _pharmacistRepo.CreatePharmacistAsync(pharmacist);

                return ApiResponse<object>.Ok(new PharmacistRegisterResponse
                {
                    StaffId = staffId,
                    UserId = user.UserId,
                    Name = request.Name,
                    LicenseNumber = pharmacist.LicenseNumber,
                    Qualification = pharmacist.Qualification,
                    Certification = pharmacist.Certification,
                    YearsOfExperience = pharmacist.YearsOfExperience
                });
            }
            catch (Exception ex)
            {
                return ApiResponse<object>.Fail($"An error occurred: {ex.Message}", 500);
            }
        }

        public async Task<ApiResponse<object>> RetrieveAllPharmacistsAsync()
        {
            try
            {
                var pharmacists = await _pharmacistRepo.GetAllPharmacistsAsync();

                if (pharmacists.Count == 0)
                {
                    return ApiResponse<object>.Fail("No pharmacist found", 404);
                }

                var result = pharmacists.Select(pharmacist => new PharmacistRetrieveResponse
                {
                    StaffId = pharmacist.StaffId,
                    UserId = pharmacist.UserId,
                    Username = pharmacist.User.Username,
                    Name = pharmacist.User.Name ?? string.Empty,
                    Email = pharmacist.User.Email,
                    Phone = pharmacist.User.Phone ?? string.Empty,
                    Status = pharmacist.User.Status,
                    Role = pharmacist.Role,
                    LicenseNumber = pharmacist.LicenseNumber,
                    Qualification = pharmacist.Qualification,
                    Certification = pharmacist.Certification,
                    YearsOfExperience = pharmacist.YearsOfExperience
                }).ToList();

                return ApiResponse<object>.Ok(result);
            }
            catch (Exception ex)
            {
                return ApiResponse<object>.Fail($"An error occurred: {ex.Message}", 500);
            }
        }

        public async Task<ApiResponse<object>> RetrievePharmacistByIdAsync(string staffId)
        {
            try
            {
                var pharmacist = await _pharmacistRepo.GetPharmacistByIdAsync(staffId);

                if (pharmacist == null)
                {
                    return ApiResponse<object>.Fail("Pharmacist not found", 404);
                }

                return ApiResponse<object>.Ok(null);
            }
            catch (Exception ex)
            {
                return ApiResponse<object>.Fail($"An error occurred: {ex.Message}", 500);
            }
        }

        public async Task<ApiResponse<object>> UpdatePharmacistAsync(
            PharmacistUpdateRequest request)
        {
            try
            {
                var pharmacist = await _pharmacistRepo.GetPharmacistByIdAsync(request.StaffId);

                if (pharmacist == null)
                {
                    return ApiResponse<object>.Fail("Pharmacist not found", 404);
                }

                if (request.LicenseNumber != null)
                {
                    pharmacist.LicenseNumber = request.LicenseNumber;
                }

                if (request.Qualification != null)
                {
                    pharmacist.Qualification = request.Qualification;
                }

                if (request.Certification != null)
                {
                    pharmacist.Certification = request.Certification;
                }

                if (request.YearsOfExperience != null)
                {
                    pharmacist.YearsOfExperience = request.YearsOfExperience.Value;
                }

                var success = await _pharmacistRepo.UpdatePharmacistAsync(pharmacist);

                if (!success)
                {
                    return ApiResponse<object>.Fail("Failed to update pharmacist", 500);
                }

                return ApiResponse<object>.Ok(new PharmacistRetrieveResponse
                {
                    StaffId = pharmacist.StaffId,
                    UserId = pharmacist.UserId,
                    Username = pharmacist.User.Username,
                    Name = pharmacist.User.Name ?? string.Empty,
                    Email = pharmacist.User.Email,
                    Phone = pharmacist.User.Phone ?? string.Empty,
                    Status = pharmacist.User.Status,
                    Role = pharmacist.Role,
                    LicenseNumber = pharmacist.LicenseNumber,
                    Qualification = pharmacist.Qualification,
                    Certification = pharmacist.Certification,
                    YearsOfExperience = pharmacist.YearsOfExperience
                });
            }
            catch (Exception ex)
            {
                return ApiResponse<object>.Fail($"An error occurred: {ex.Message}", 500);
            }
        }
    }
}
