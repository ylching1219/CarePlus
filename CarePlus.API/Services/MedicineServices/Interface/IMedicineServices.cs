using CarePlus.API.DTO.MedicineDTO;

namespace CarePlus.API.Services.MedicineServices.Interface
{
    public interface IMedicineServices
    {
        Task<ApiResponse<object>> CreateMedicineAsync(MedicineRegisterRequest request);
        Task<ApiResponse<object>> RetrieveAllMedicinesAsync();
        Task<ApiResponse<object>> RetrieveMedicineByIdAsync(string medicineId);
        Task<ApiResponse<object>> UpdateMedicineAsync(MedicineUpdateRequest request);
        Task<ApiResponse<object>> SaveImageAsync(string medicineId, IFormFile? file);
        Task<string?> ResolveStoredImageAsync(string medicineId);
    }
}
