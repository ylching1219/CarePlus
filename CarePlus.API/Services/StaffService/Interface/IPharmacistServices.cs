using CarePlus.API.DTO.StaffDTO;

namespace CarePlus.API.Services.StaffService.Interface
{
    public interface IPharmacistServices
    {
        Task<ApiResponse<object>> RegisterPharmacistAsync(PharmacistRegisterRequest request);
        Task<ApiResponse<object>> RetrieveAllPharmacistsAsync();
        Task<ApiResponse<object>> RetrievePharmacistByIdAsync(string staffId);
        Task<ApiResponse<object>> UpdatePharmacistAsync(PharmacistUpdateRequest request);
    }
}
