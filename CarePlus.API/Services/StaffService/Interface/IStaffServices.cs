using CarePlus.API.DTO.StaffDTO;

namespace CarePlus.API.Services.StaffService.Interface
{
    public interface IStaffServices
    {
        Task<ApiResponse<object>> RegisterStaffAsync(StaffRegisterRequest request);

        Task<ApiResponse<object>> RetrieveAllStaffAsync();

        Task<ApiResponse<object>> RetrieveStaffByIdAsync(string id);

        Task<ApiResponse<object>> RetrieveStaffByRoleAsync(string role);

        Task<ApiResponse<object>> UpdateStaffAsync(StaffUpdateRequest request);
    }
}
