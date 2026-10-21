using CarePlus.API.DTO.MedicineBatchDTO;

namespace CarePlus.API.Services.MedicineBatchServices.Interface
{
    public interface IMedicineBatchServices
    {
        Task<ApiResponse<object>> CreateMedicineBatchAsync(
            MedicineBatchRegisterRequest request);

        Task<ApiResponse<object>> RetrieveAllMedicineBatchesAsync();

        Task<ApiResponse<object>> RetrieveMedicineBatchByIdAsync(string batchId);

        Task<ApiResponse<object>> UpdateMedicineBatchAsync(
            MedicineBatchUpdateRequest request);
    }
}
