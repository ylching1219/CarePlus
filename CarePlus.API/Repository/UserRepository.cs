using CarePlus.API.Data;
using CarePlus.API.Models.Payment;
using CarePlus.API.Models.User;
using CarePlus.API.Repository.Interface;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace CarePlus.API.Repository
{
    public class UserRepository : IUserRepository
    {

        private readonly AppDbContext _context;

        public UserRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<long> CreateUserAsync(User user)
        {
            await  _context.Users.AddAsync(user);
            await  _context.SaveChangesAsync();

            return user.UserId;
        }

        public async Task<List<User>> GetAllUsersAsync()
        {
            List<User> users = new List<User>();
            users = await _context.Users.ToListAsync();

            return users;
        }

        public async Task<List<User>> GetAllUsersByAsync(Expression<Func<User, bool>> predicate)
        {
            return await _context.Users.Where(predicate).ToListAsync();
        }

        public async Task<User?> GetUserByIdAsync(long id)
        {
            return await _context.Users.FirstOrDefaultAsync(u => u.UserId == id);
        }

        public async Task<bool> UpdateUserAsync(User user)
        {
            _context.Users.Update(user);
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task AddLoginActivityAsync(LoginActivity activity)
        {
            await _context.LoginActivities.AddAsync(activity);
            await _context.SaveChangesAsync();
        }

        public async Task<bool> TouchLoginActivityAsync(long userId, string device, string ipAddress, DateTime createdAt)
        {
            var matches = await _context.LoginActivities
                .Where(activity => activity.UserId == userId && activity.Device == device)
                .OrderByDescending(activity => activity.CreatedAt)
                .ToListAsync();
            if (matches.Count == 0)
            {
                return false;
            }

            matches[0].IpAddress = ipAddress;
            matches[0].CreatedAt = createdAt;
            if (matches.Count > 1)
            {
                _context.LoginActivities.RemoveRange(matches.Skip(1));
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<List<LoginActivity>> GetRecentLoginActivityAsync(long userId)
        {
            var rows = await _context.LoginActivities
                .Where(activity => activity.UserId == userId)
                .OrderByDescending(activity => activity.CreatedAt)
                .ToListAsync();

            return rows
                .GroupBy(activity => activity.Device)
                .Select(group => group.First())
                .OrderByDescending(activity => activity.CreatedAt)
                .Take(20)
                .ToList();
        }

        public async Task<AppSetting?> GetAppSettingAsync(long userId)
        {
            return await _context.AppSettings.FirstOrDefaultAsync(setting => setting.UserId == userId);
        }

        public async Task AddAppSettingAsync(AppSetting setting)
        {
            await _context.AppSettings.AddAsync(setting);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAppSettingAsync(AppSetting setting)
        {
            _context.AppSettings.Update(setting);
            await _context.SaveChangesAsync();
        }

        public async Task<List<PaymentCard>> GetPaymentCardsAsync(long userId)
        {
            return await _context.PaymentCards
                .Where(card => card.UserId == userId)
                .OrderByDescending(card => card.IsDefault)
                .ThenBy(card => card.Id)
                .ToListAsync();
        }

        public async Task<PaymentCard?> GetPaymentCardAsync(long id, long userId)
        {
            return await _context.PaymentCards.FirstOrDefaultAsync(card => card.Id == id && card.UserId == userId);
        }

        public async Task AddPaymentCardAsync(PaymentCard card)
        {
            await _context.PaymentCards.AddAsync(card);
            await _context.SaveChangesAsync();
        }

        public async Task SavePaymentCardsAsync()
        {
            await _context.SaveChangesAsync();
        }

        public async Task DeletePaymentCardAsync(PaymentCard card)
        {
            _context.PaymentCards.Remove(card);
            await _context.SaveChangesAsync();
        }
    }
}
