using CarePlus.API.Data;
using CarePlus.API.Models.User.Staff.Admin;
using CarePlus.API.Repository.Interface;
using Microsoft.EntityFrameworkCore;

namespace CarePlus.API.Repository
{
    public class AdminRepository : IAdminRepository
    {
        private readonly AppDbContext _context;

        public AdminRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<string> CreateAdminAsync(Admin admin)
        {
            await _context.Admins.AddAsync(admin);
            await _context.SaveChangesAsync();

            return admin.StaffId;
        }

        public async Task<Admin?> GetAdminByIdAsync(string staffId)
        {
            return await _context.Admins
                .Include(a => a.User)
                .FirstOrDefaultAsync(a => a.StaffId == staffId);
        }

        public async Task<List<Admin>> GetAllAdminsAsync()
        {
            return await _context.Admins
                .Include(a => a.User)
                .ToListAsync();
        }

        public async Task<bool> UpdateAdminAsync(Admin admin)
        {
            _context.Admins.Update(admin);
            await _context.SaveChangesAsync();

            return true;
        }
    }
}
