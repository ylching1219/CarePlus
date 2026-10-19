using CarePlus.API.DTO.StaffDTO;
using CarePlus.API.DTO.UserDTO;
using CarePlus.API.Models.User.Staff.Doctor;
using CarePlus.API.Repository.Interface;
using CarePlus.API.Services.CounterServices.Interface;
using CarePlus.API.Services.StaffService.Interface;
using CarePlus.API.Services.UserServices.Interface;

namespace CarePlus.API.Services.StaffService
{
    public class DoctorServices : IDoctorServices
    {
        private readonly IDoctorRepository _doctorRepo;
        private readonly IUserServices _userService;
        private readonly ICounterServices _counterService;

        public DoctorServices(
            IDoctorRepository doctorRepo,
            IUserServices userService,
            ICounterServices counterService)
        {
            _doctorRepo = doctorRepo;
            _userService = userService;
            _counterService = counterService;
        }

        public async Task<ApiResponse<object>> RegisterDoctorAsync(
            DoctorRegisterRequest request)
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

                var doctor = new Doctor
                {
                    StaffId = await _counterService.GenerateNextIdAsync("StaffId"),
                    UserId = user.UserId,
                    Role = "Doctor",
                    LicenseNumber = request.LicenseNumber,
                    Specialization = request.Specialization,
                    Qualification = request.Qualification,
                    YearsOfExperience = request.YearsOfExperience
                };

                var staffId = await _doctorRepo.CreateDoctorAsync(doctor);

                return ApiResponse<object>.Ok(new DoctorRegisterResponse
                {
                    StaffId = staffId,
                    UserId = user.UserId,
                    Name = request.Name,
                    LicenseNumber = doctor.LicenseNumber,
                    Specialization = doctor.Specialization,
                    Qualification = doctor.Qualification,
                    YearsOfExperience = doctor.YearsOfExperience
                });
            }
            catch (Exception ex)
            {
                return ApiResponse<object>.Fail($"An error occurred: {ex.Message}", 500);
            }
        }

        public async Task<ApiResponse<object>> RetrieveAllDoctorsAsync()
        {
            try
            {
                var doctors = await _doctorRepo.GetAllDoctorsAsync();

                if (doctors.Count == 0)
                {
                    return ApiResponse<object>.Fail("No doctor found", 404);
                }

                var result = doctors.Select(doctor => new DoctorRetrieveResponse
                {
                    StaffId = doctor.StaffId,
                    UserId = doctor.UserId,
                    Username = doctor.User.Username,
                    Name = doctor.User.Name ?? string.Empty,
                    Email = doctor.User.Email,
                    Phone = doctor.User.Phone ?? string.Empty,
                    Status = doctor.User.Status,
                    Role = doctor.Role,
                    LicenseNumber = doctor.LicenseNumber,
                    Specialization = doctor.Specialization,
                    Qualification = doctor.Qualification,
                    YearsOfExperience = doctor.YearsOfExperience
                }).ToList();

                return ApiResponse<object>.Ok(result);
            }
            catch (Exception ex)
            {
                return ApiResponse<object>.Fail($"An error occurred: {ex.Message}", 500);
            }
        }

        public async Task<ApiResponse<object>> RetrieveDoctorByIdAsync(string staffId)
        {
            try
            {
                var doctor = await _doctorRepo.GetDoctorByIdAsync(staffId);

                if (doctor == null)
                {
                    return ApiResponse<object>.Fail("Doctor not found", 404);
                }

                return ApiResponse<object>.Ok(null);
            }
            catch (Exception ex)
            {
                return ApiResponse<object>.Fail($"An error occurred: {ex.Message}", 500);
            }
        }

        public async Task<ApiResponse<object>> UpdateDoctorAsync(DoctorUpdateRequest request)
        {
            try
            {
                var doctor = await _doctorRepo.GetDoctorByIdAsync(request.StaffId);

                if (doctor == null)
                {
                    return ApiResponse<object>.Fail("Doctor not found", 404);
                }

                if (request.LicenseNumber != null)
                {
                    doctor.LicenseNumber = request.LicenseNumber;
                }

                if (request.Specialization != null)
                {
                    doctor.Specialization = request.Specialization;
                }

                if (request.Qualification != null)
                {
                    doctor.Qualification = request.Qualification;
                }

                if (request.YearsOfExperience != null)
                {
                    doctor.YearsOfExperience = request.YearsOfExperience.Value;
                }

                var success = await _doctorRepo.UpdateDoctorAsync(doctor);

                if (!success)
                {
                    return ApiResponse<object>.Fail("Failed to update doctor", 500);
                }

                return ApiResponse<object>.Ok(new DoctorRetrieveResponse
                {
                    StaffId = doctor.StaffId,
                    UserId = doctor.UserId,
                    Username = doctor.User.Username,
                    Name = doctor.User.Name ?? string.Empty,
                    Email = doctor.User.Email,
                    Phone = doctor.User.Phone ?? string.Empty,
                    Status = doctor.User.Status,
                    Role = doctor.Role,
                    LicenseNumber = doctor.LicenseNumber,
                    Specialization = doctor.Specialization,
                    Qualification = doctor.Qualification,
                    YearsOfExperience = doctor.YearsOfExperience
                });
            }
            catch (Exception ex)
            {
                return ApiResponse<object>.Fail($"An error occurred: {ex.Message}", 500);
            }
        }
    }
}
