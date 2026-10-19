using CarePlus.API.DTO.StaffDTO;

namespace CarePlus.API.Services.StaffService.Interface
{
    public interface IAdminServices
    {
        Task<ApiResponse<object>> RegisterAdminAsync(AdminRegisterRequest request);
        Task<ApiResponse<object>> RetrieveAllAdminsAsync();
        Task<ApiResponse<object>> RetrieveAdminByIdAsync(string staffId);
        Task<ApiResponse<object>> UpdateAdminAsync(AdminUpdateRequest request);
    }
}
