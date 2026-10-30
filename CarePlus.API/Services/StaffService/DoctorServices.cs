using CarePlus.API.Data;
using CarePlus.API.DTO.StaffDTO;
using CarePlus.API.DTO.UserDTO;
using CarePlus.API.Models.User.Staff.Doctor;
using CarePlus.API.Repository.Interface;
using CarePlus.API.Services.CounterServices.Interface;
using CarePlus.API.Services.StaffService.Interface;
using CarePlus.API.Services.UserServices.Interface;
using Microsoft.EntityFrameworkCore;

namespace CarePlus.API.Services.StaffService
{
    public class DoctorServices : IDoctorServices
    {
        private readonly IDoctorRepository _doctorRepo;
        private readonly AppDbContext _db;
        private readonly IUserServices _userService;
        private readonly ICounterServices _counterService;

        public DoctorServices(
            IDoctorRepository doctorRepo,
            AppDbContext db,
            IUserServices userService,
            ICounterServices counterService)
        {
            _doctorRepo = doctorRepo;
            _db = db;
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
                await _userService.EnsureAppSettingAsync(user.UserId, true);

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

        public async Task<ApiResponse<List<PublicDoctorResponse>>> RetrievePublicDoctorsAsync()
        {
            var doctors = await _doctorRepo.GetAllDoctorsAsync();
            var activeDoctors = doctors
                .Where(doctor => string.Equals(doctor.User.Status, "Active", StringComparison.OrdinalIgnoreCase))
                .OrderBy(doctor => doctor.User.Name)
                .ToList();

            if (activeDoctors.Count == 0)
            {
                return ApiResponse<List<PublicDoctorResponse>>.Fail("No doctor found", 404);
            }

            var result = new List<PublicDoctorResponse>();
            foreach (var doctor in activeDoctors)
            {
                result.Add(await ToPublicDoctorAsync(doctor));
            }

            return ApiResponse<List<PublicDoctorResponse>>.Ok(result);
        }

        public async Task<ApiResponse<PublicDoctorResponse>> RetrievePublicDoctorByIdAsync(string staffId)
        {
            var doctor = await _doctorRepo.GetDoctorByIdAsync(staffId);
            if (doctor == null || !string.Equals(doctor.User.Status, "Active", StringComparison.OrdinalIgnoreCase))
            {
                return ApiResponse<PublicDoctorResponse>.Fail("Doctor not found", 404);
            }

            return ApiResponse<PublicDoctorResponse>.Ok(await ToPublicDoctorAsync(doctor));
        }

        private async Task<PublicDoctorResponse> ToPublicDoctorAsync(Doctor doctor)
        {
            var scheduleRows = await _db.WorkSchedules
                .AsNoTracking()
                .Where(schedule => schedule.StaffId == doctor.StaffId && schedule.Status == "Active")
                .OrderBy(schedule => schedule.WorkingDate)
                .ThenBy(schedule => schedule.StartTime)
                .ToListAsync();

            return new PublicDoctorResponse
            {
                StaffId = doctor.StaffId,
                Name = doctor.User.Name ?? string.Empty,
                Specialization = doctor.Specialization,
                Qualification = doctor.Qualification,
                YearsOfExperience = doctor.YearsOfExperience,
                Schedules = scheduleRows.Select(schedule => new PublicDoctorScheduleResponse
                {
                    WorkingDate = schedule.WorkingDate,
                    StartTime = schedule.StartTime.ToString(),
                    EndTime = schedule.EndTime.ToString(),
                }).ToList(),
            };
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

                if (!string.IsNullOrWhiteSpace(request.LicenseNumber))
                {
                    doctor.LicenseNumber = request.LicenseNumber.Trim();
                }

                if (!string.IsNullOrWhiteSpace(request.Specialization))
                {
                    doctor.Specialization = request.Specialization.Trim();
                }

                if (!string.IsNullOrWhiteSpace(request.Qualification))
                {
                    doctor.Qualification = request.Qualification.Trim();
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
