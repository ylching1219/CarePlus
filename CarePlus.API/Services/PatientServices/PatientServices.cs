using CarePlus.API.DTO.PatientDTO;
using CarePlus.API.DTO.UserDTO;
using CarePlus.API.Models.Patient;
using CarePlus.API.Repository.Interface;
using CarePlus.API.Services.CounterServices.Interface;
using CarePlus.API.Services.PatientServices.Interface;
using CarePlus.API.Services.UserServices.Interface;

namespace CarePlus.API.Services.PatientServices
{
    public class PatientServices : IPatientServices
    {
        private readonly IPatientRepository _patientRepo;
        private readonly IUserServices _userService;
        private readonly ICounterServices _counterService;

        public PatientServices(
            IPatientRepository patientRepo,
            IUserServices userServices,
            ICounterServices counterService)
        {
            _patientRepo = patientRepo;
            _userService = userServices;
            _counterService = counterService;
        }

        public async Task<ApiResponse<object>> RegisterPatientAsync(PatientRegisterRequest request)
        {
            try {

                if(request.Gender != 'F' && request.Gender != 'M')
                {
                    return ApiResponse<object>.Fail("Invalid Gender Type.",400);
                }

                var userResponse = await _userService.RegisterUserAsync(request);

                if (!userResponse.IsSuccess)
                {
                    return ApiResponse<object>.Fail(userResponse.RespMessage,userResponse.RespCode);
                }

                var user = userResponse.Data as UserRegisterResponse;
                if (user == null) 
                {
                    return ApiResponse<object>.Fail("Invalid user response", 500);
                }

                var patient = new Patient
                {
                    PatientId = await _counterService.GenerateNextIdAsync("PatientId"),
                    UserId = user.userId,
                    Gender = request.Gender
                };

                var patientId = await _patientRepo.CreatePatientAsync(patient);

                return ApiResponse<object>.Ok(new PatientRegisterResponse
                {
                    userId = user.userId,
                    name = request.Name,
                    patientId = patientId
                });
            }
            catch(Exception ex) 
            {
                return ApiResponse<object>.Fail($"An error occurred: {ex.Message}", 500);
            }
        }

        public async Task<ApiResponse<object>> RetreiveAllPatientAsync()
        {
            try
            {
                List<Patient> patients = await _patientRepo.GetAllPatientsAsync();

                if (patients.Count > 0)
                {
                    var result = patients.Select(patient => new PatientRetrieveResponse
                    {
                        PatientId = patient.PatientId,
                        userId = patient.UserId,
                        username = patient.User.Username,
                        name = patient.User.Name ?? string.Empty,
                        email = patient.User.Email,
                        phone = patient.User.Phone ?? string.Empty,
                        status = patient.User.Status ?? string.Empty,
                        Gender = patient.Gender == 'M' ? "Male" : "Female",
                        DateOfBirth = patient.DateOfBirth,
                        Address = patient.Address ?? string.Empty,
                    }).ToList();

                    return ApiResponse<object>.Ok(result);
                }
                return ApiResponse<object>.Fail("No patients found", 404);
            }catch(Exception ex)
            {
                return ApiResponse<object>.Fail($"{ex.Message}", 500);
            }
        }

        public Task<ApiResponse<object>> RetreivePatientByIdAsync(string id)
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
    }


}
