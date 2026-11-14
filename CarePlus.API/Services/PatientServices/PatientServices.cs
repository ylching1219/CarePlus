using CarePlus.API.DTO.PatientDTO;
using CarePlus.API.DTO.UserDTO;
using CarePlus.API.Data;
using CarePlus.API.Models.Patient;
using CarePlus.API.Repository.Interface;
using CarePlus.API.Services.CounterServices.Interface;
using CarePlus.API.Services.PatientServices.Interface;
using CarePlus.API.Services.UserServices.Interface;
using CarePlus.API.Extension;
using CarePlus.API.Services.RegistrationEmailVerificationServices.Interface;

namespace CarePlus.API.Services.PatientServices
{
    public class PatientServices : IPatientServices
    {
        private readonly IPatientRepository _patientRepo;
        private readonly IUserServices _userService;
        private readonly ICounterServices _counterService;
        private readonly IRegistrationEmailVerificationServices _registrationEmailVerificationServices;
        private readonly AppDbContext _db;

        public PatientServices(
            IPatientRepository patientRepo,
            IUserServices userServices,
            ICounterServices counterService,
            IRegistrationEmailVerificationServices registrationEmailVerificationServices,
            AppDbContext db)
        {
            _patientRepo = patientRepo;
            _userService = userServices;
            _counterService = counterService;
            _registrationEmailVerificationServices = registrationEmailVerificationServices;
            _db = db;
        }

        public async Task<ApiResponse<object>> RegisterPatientAsync(PatientRegisterRequest request)
        {
            await using var transaction = await _db.Database.BeginTransactionAsync();

            try {
                if (!await _registrationEmailVerificationServices.IsVerifiedAsync(
                    request.Email,
                    request.EmailVerificationChallengeId,
                    request.EmailVerificationToken))
                {
                    return ApiResponse<object>.Fail("Verify your email address before registering.", 400);
                }

                if (!TryReadIdentityCard(request.IdentityCardNumber, out var identityCard, out var dateOfBirth, out var gender))
                {
                    return ApiResponse<object>.Fail("Identity card number is invalid.", 400);
                }

                var errors = await _userService.ValidateUserAsync(request);
                if (await _patientRepo.IdentityCardExistsAsync(identityCard))
                {
                    errors.Add("Identity card number already exists.");
                }

                if (errors.Count > 0)
                {
                    return ApiResponse<object>.Fail(string.Join("\n", errors), 400, errors);
                }

                var userResponse = await _userService.RegisterUserAsync(request, emailVerified: true);

                if (!userResponse.IsSuccess)
                {
                    return userResponse;
                }

                var user = userResponse.Data as UserRegisterResponse;
                if (user == null) 
                {
                    return ApiResponse<object>.Fail("Invalid user response", 500);
                }

                var patient = new Patient
                {
                    PatientId = await _counterService.GenerateNextIdAsync("PatientId"),
                    UserId = user.UserId,
                    IdentityCardNumber = identityCard,
                    DateOfBirth = dateOfBirth.ToDateTime(TimeOnly.MinValue),
                    Gender = gender,
                    AnnouncementStartAt = DateTimeHelper.MalaysiaNow()
                };

                var patientId = await _patientRepo.CreatePatientAsync(patient);
                await _userService.EnsureAppSettingAsync(user.UserId, false);
                await _registrationEmailVerificationServices.ConsumeAsync(request.EmailVerificationChallengeId);
                await _db.SaveChangesAsync();

                await transaction.CommitAsync();

                return ApiResponse<object>.Ok(new PatientRegisterResponse
                {
                    UserId = user.UserId,
                    Name = request.Name,
                    PatientId = patientId,
                    EmailVerificationRequired = false,
                    VerificationChallengeId = string.Empty,
                    MaskedEmail = string.Empty,
                    VerificationExpiresInSeconds = 0,
                });
            }
            catch (Exception) 
            {
                await transaction.RollbackAsync();
                return ApiResponse<object>.Fail("An unexpected error occurred.", 500);
            }
        }

        public async Task<ApiResponse<object>> RetrieveAllPatientAsync()
        {
            try
            {
                List<Patient> patients = await _patientRepo.GetAllPatientsAsync();

                if (patients.Count > 0)
                {
                    var result = patients.Select(patient => new PatientRetrieveResponse
                    {
                        PatientId = patient.PatientId,
                        UserId = patient.UserId,
                        Username = patient.User.Username,
                        Name = patient.User.Name ?? string.Empty,
                        Email = patient.User.Email,
                        Phone = patient.User.Phone ?? string.Empty,
                        Status = patient.User.Status ?? string.Empty,
                        Gender = patient.Gender == 'M' ? "Male" : patient.Gender == 'F' ? "Female" : "Other",
                        DateOfBirth = patient.DateOfBirth.HasValue ? DateOnly.FromDateTime(patient.DateOfBirth.Value) : null,
                        Address = patient.Address ?? string.Empty,
                    }).ToList();

                    return ApiResponse<object>.Ok(result);
                }
                return ApiResponse<object>.Fail("No patients found", 404);
            }catch (Exception)
            {
                return ApiResponse<object>.Fail("An unexpected error occurred.", 500);
            }
        }

        public Task<ApiResponse<object>> RetrievePatientByIdAsync(string id)
        {
            throw new NotImplementedException();
        }

        public Task<ApiResponse<object>> RetrieveAllActivePatientAsync()
        {
            throw new NotImplementedException();
        }

        public Task<ApiResponse<object>> UpdatePatientAsync()
        {
            throw new NotImplementedException();
        }

        private static bool TryReadIdentityCard(string value, out string digits, out DateOnly dateOfBirth, out char gender)
        {
            digits = new string(value.Where(char.IsDigit).ToArray());
            dateOfBirth = default;
            gender = 'M';
            if (digits.Length != 12)
            {
                return false;
            }

            if (!int.TryParse(digits[..2], out var year) || !int.TryParse(digits[2..4], out var month) || !int.TryParse(digits[4..6], out var day))
            {
                return false;
            }

            var fullYear = year <= 29 ? 2000 + year : 1900 + year;
            try
            {
                dateOfBirth = new DateOnly(fullYear, month, day);
                gender = (digits[^1] - '0') % 2 == 1 ? 'M' : 'F';
                return true;
            }
            catch (ArgumentOutOfRangeException)
            {
                return false;
            }
        }
    }


}
