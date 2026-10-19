using CarePlus.API.Models.User.Staff;
using System.Linq.Expressions;

namespace CarePlus.API.Repository.Interface
{
    public interface IStaffRepository
    {
        Task<Staff?> GetStaffByIdAsync(string id);
        Task<Staff?> GetStaffByUserIdAsync(long userId);
        Task<string> CreateStaffAsync(Staff staff);
        Task<bool> UpdateStaffAsync(Staff staff);
        Task<List<Staff>> GetAllStaffsAsync();
        Task<List<Staff>> GetAllStaffsByAsync(Expression<Func<Staff, bool>> predicate);
    }
}
