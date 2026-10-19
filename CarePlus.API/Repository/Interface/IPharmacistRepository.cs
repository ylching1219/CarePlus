using CarePlus.API.Models.User.Staff.Pharmacist;

namespace CarePlus.API.Repository.Interface
{
    public interface IPharmacistRepository
    {
        Task<string> CreatePharmacistAsync(Pharmacist pharmacist);
        Task<Pharmacist?> GetPharmacistByIdAsync(string staffId);
        Task<List<Pharmacist>> GetAllPharmacistsAsync();
        Task<bool> UpdatePharmacistAsync(Pharmacist pharmacist);
    }
}
