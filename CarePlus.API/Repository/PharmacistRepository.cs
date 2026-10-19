using CarePlus.API.Data;
using CarePlus.API.Models.User.Staff.Pharmacist;
using CarePlus.API.Repository.Interface;
using Microsoft.EntityFrameworkCore;

namespace CarePlus.API.Repository
{
    public class PharmacistRepository : IPharmacistRepository
    {
        private readonly AppDbContext _context;

        public PharmacistRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<string> CreatePharmacistAsync(Pharmacist pharmacist)
        {
            await _context.Pharmacists.AddAsync(pharmacist);
            await _context.SaveChangesAsync();

            return pharmacist.StaffId;
        }

        public async Task<Pharmacist?> GetPharmacistByIdAsync(string staffId)
        {
            return await _context.Pharmacists
                .Include(p => p.User)
                .FirstOrDefaultAsync(p => p.StaffId == staffId);
        }

        public async Task<List<Pharmacist>> GetAllPharmacistsAsync()
        {
            return await _context.Pharmacists
                .Include(p => p.User)
                .ToListAsync();
        }

        public async Task<bool> UpdatePharmacistAsync(Pharmacist pharmacist)
        {
            _context.Pharmacists.Update(pharmacist);
            await _context.SaveChangesAsync();

            return true;
        }
    }
}
