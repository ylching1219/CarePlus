using CarePlus.API.Models.User.Staff.Admin;

namespace CarePlus.API.Repository.Interface
{
    public interface IAdminRepository
    {
        Task<string> CreateAdminAsync(Admin admin);
        Task<Admin?> GetAdminByIdAsync(string staffId);
        Task<List<Admin>> GetAllAdminsAsync();
        Task<bool> UpdateAdminAsync(Admin admin);
    }
}
