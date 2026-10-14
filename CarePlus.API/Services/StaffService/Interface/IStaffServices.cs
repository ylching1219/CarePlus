using CarePlus.API.DTO.StaffDTO;

namespace CarePlus.API.Services.StaffService.Interface
{
    public interface IStaffServices
    {
        Task<ApiResponse<object>> RegisterStaffAsync(StaffRegisterRequest request);

        Task<ApiResponse<object>> RetreiveAllStaffAsync();

        Task<ApiResponse<object>> RetreiveStaffByIdAsync(long id);

        Task<ApiResponse<object>> RetrieveAllActiveStaffAsync();

        Task<ApiResponse<object>> UpdateStaffAsync();
    }
}
