using CarePlus.API.Models.MedicineBatch;

namespace CarePlus.API.Repository.Interface
{
    public interface IMedicineBatchRepository
    {
        Task<string> CreateMedicineBatchAsync(MedicineBatch medicineBatch);
        Task<MedicineBatch?> GetMedicineBatchByIdAsync(string batchId);
        Task<List<MedicineBatch>> GetAllMedicineBatchesAsync();
        Task<bool> UpdateMedicineBatchAsync(MedicineBatch medicineBatch);
    }
}
