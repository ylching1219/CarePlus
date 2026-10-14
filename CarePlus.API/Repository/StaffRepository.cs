using CarePlus.API.Data;
using CarePlus.API.Models.User.Staff;
using CarePlus.API.Repository.Interface;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace CarePlus.API.Repository
{
    public class StaffRepository : IStaffRepository
    {
        private readonly AppDbContext _context;

        public StaffRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<string> CreateStaffAsync(Staff staff)
        {
            await _context.Staffs.AddAsync(staff);
            await _context.SaveChangesAsync();

            return staff.StaffId;
        }

        public async Task<List<Staff>> GetAllStaffsAsync()
        {
            return await _context.Staffs.Include(s => s.User).ToListAsync();
        }

        public async Task<List<Staff>> GetAllStaffsByAsync(
            Expression<Func<Staff, bool>> predicate)
        {
            return await _context.Staffs.Include(s => s.User).Where(predicate).ToListAsync();
        }

        public async Task<Staff?> GetStaffByIdAsync(string id)
        {
            return await _context.Staffs.Include(s => s.User).FirstOrDefaultAsync(s => s.StaffId == id);
        }

        public async Task<bool> UpdateStaffAsync(Staff staff)
        {
            _context.Staffs.Update(staff);
            await _context.SaveChangesAsync();

            return true;
        }
    }
}
