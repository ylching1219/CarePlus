using CarePlus.API.DTO.UserDTO;
using CarePlus.API.Models.Payment;
using CarePlus.API.Models.User;
using System.Linq.Expressions;

namespace CarePlus.API.Repository.Interface
{
    public interface IUserRepository
    {
        Task<User?> GetUserByIdAsync(long id);
        Task<long> CreateUserAsync(User user);
        Task<bool> UpdateUserAsync(User user);
        Task<List<User>> GetAllUsersAsync();
        Task<List<User>> GetAllUsersByAsync(Expression<Func<User,bool>> predicate);
        Task AddLoginActivityAsync(LoginActivity activity);
        Task<bool> TouchLoginActivityAsync(long userId, string device, string ipAddress, DateTime createdAt);
        Task<List<LoginActivity>> GetRecentLoginActivityAsync(long userId);
        Task<AppSetting?> GetAppSettingAsync(long userId);
        Task AddAppSettingAsync(AppSetting setting);
        Task UpdateAppSettingAsync(AppSetting setting);
        Task<List<PaymentCard>> GetPaymentCardsAsync(long userId);
        Task<PaymentCard?> GetPaymentCardAsync(long id, long userId);
        Task AddPaymentCardAsync(PaymentCard card);
        Task SavePaymentCardsAsync();
        Task DeletePaymentCardAsync(PaymentCard card);
    }
}
