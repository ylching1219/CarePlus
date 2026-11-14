using CarePlus.API.Models.Medicine;

namespace CarePlus.API.Repository.Interface
{
    public interface IMedicineRepository
    {
        Task<string> CreateMedicineAsync(Medicine medicine);
        Task<Medicine?> GetMedicineByIdAsync(string medicineId);
        Task<List<Medicine>> GetAllMedicinesAsync();
        Task<Dictionary<string, int>> AvailableStockAsync();
        Task<bool> UpdateMedicineAsync(Medicine medicine);
    }
}
