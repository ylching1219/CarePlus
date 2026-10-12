using CarePlus.API.DTO.UserDTO;
using CarePlus.API.Models.User;
using System.Linq.Expressions;

namespace CarePlus.API.Repository.Interface
{
    public interface IUserRepository
    {
        Task<User?> GetUserByIdAsync(int id);
        Task<long> CreateUserAsync(User user);
        Task<bool> UpdateUserAsync(User user);
        Task<List<User>> GetAllUsersAsync();
        Task<List<User>> GetAllUsersByAsync(Expression<Func<User,bool>> predicate);
    }
}
