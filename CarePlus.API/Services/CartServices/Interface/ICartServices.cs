using CarePlus.API.DTO.CartDTO;

namespace CarePlus.API.Services.CartServices.Interface
{
    public interface ICartServices
    {
        Task<ApiResponse<CartStateResponse>> ListAsync(long userId);
        Task<ApiResponse<CartStateResponse>> SetQtyAsync(long userId, CartQtyRequest request);
        Task<ApiResponse<CartStateResponse>> RemoveAsync(long userId, IReadOnlyCollection<string> medicineIds);
        Task<ApiResponse<CartStateResponse>> ClearAsync(long userId);
    }
}
