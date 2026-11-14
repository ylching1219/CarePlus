using CarePlus.API.DTO.StaffDTO;

namespace CarePlus.API.Services.StaffService.Interface
{
    public interface IDoctorServices
    {
        Task<ApiResponse<object>> RegisterDoctorAsync(DoctorRegisterRequest request);
        Task<ApiResponse<object>> RetrieveAllDoctorsAsync();
        Task<ApiResponse<object>> RetrieveDoctorByIdAsync(string staffId);
        Task<ApiResponse<List<PublicDoctorResponse>>> RetrievePublicDoctorsAsync();
        Task<ApiResponse<PublicDoctorResponse>> RetrievePublicDoctorByIdAsync(string staffId);
        Task<ApiResponse<object>> UpdateDoctorAsync(DoctorUpdateRequest request);
    }
}
