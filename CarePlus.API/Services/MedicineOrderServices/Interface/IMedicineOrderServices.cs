using CarePlus.API.DTO.MedicineOrderDTO;

namespace CarePlus.API.Services.MedicineOrderServices.Interface
{
    public interface IMedicineOrderServices
    {
        Task<ApiResponse<List<MedicineOrderResponse>>> ListAsync(long userId);
        Task<ApiResponse<List<MedicineOrderResponse>>> ListPaidAsync(long userId);
        Task<ApiResponse<MedicineOrderResponse>> PlaceAsync(long userId, MedicineOrderPlaceRequest request);
        Task<ApiResponse<MedicineOrderResponse>> PayAsync(long userId, string orderId, MedicineOrderPayRequest request);
        Task<ApiResponse<List<MedicineOrderResponse>>> ListForStaffAsync();
        Task<ApiResponse<MedicineOrderResponse>> UpdateStatusAsync(string orderId, string status);
        Task<ApiResponse<MapPointResponse?>> LocateAsync(string? address);
    }
}
