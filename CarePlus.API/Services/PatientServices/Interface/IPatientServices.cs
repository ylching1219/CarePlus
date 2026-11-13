using CarePlus.API.DTO.PatientDTO;
using CarePlus.API.DTO.UserDTO;

namespace CarePlus.API.Services.PatientServices.Interface
{
    public interface IPatientServices
    {
        Task<ApiResponse<object>> RegisterPatientAsync(PatientRegisterRequest request);

        Task<ApiResponse<object>> RetrieveAllPatientAsync();

        Task<ApiResponse<object>> RetrievePatientByIdAsync(string id);

        Task<ApiResponse<object>> RetrieveAllActivePatientAsync();

        Task<ApiResponse<object>> UpdatePatientAsync();
    }
}
