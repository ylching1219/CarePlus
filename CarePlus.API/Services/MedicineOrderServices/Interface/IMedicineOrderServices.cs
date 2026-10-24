using CarePlus.API.DTO.MedicineOrderDTO;

namespace CarePlus.API.Services.MedicineOrderServices.Interface
{
    public interface IMedicineOrderServices
    {
        Task<ApiResponse<List<MedicineOrderResponse>>> ListAsync(long userId);
        Task<ApiResponse<List<MedicineOrderResponse>>> ListPaidAsync(long userId);
        Task<ApiResponse<MedicineOrderResponse>> PlaceAsync(long userId, MedicineOrderPlaceRequest request);
        Task<ApiResponse<MapPointResponse?>> LocateAsync(string? address);
    }
}
