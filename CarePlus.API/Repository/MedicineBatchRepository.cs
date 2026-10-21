using CarePlus.API.Data;
using CarePlus.API.Models.MedicineBatch;
using CarePlus.API.Repository.Interface;
using Microsoft.EntityFrameworkCore;

namespace CarePlus.API.Repository
{
    public class MedicineBatchRepository : IMedicineBatchRepository
    {
        private readonly AppDbContext _context;

        public MedicineBatchRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<string> CreateMedicineBatchAsync(MedicineBatch medicineBatch)
        {
            await _context.MedicineBatches.AddAsync(medicineBatch);
            await _context.SaveChangesAsync();

            return medicineBatch.BatchId;
        }

        public async Task<MedicineBatch?> GetMedicineBatchByIdAsync(string batchId)
        {
            return await _context.MedicineBatches
                .Include(batch => batch.Medicine)
                .FirstOrDefaultAsync(batch => batch.BatchId == batchId);
        }

        public async Task<List<MedicineBatch>> GetAllMedicineBatchesAsync()
        {
            return await _context.MedicineBatches
                .Include(batch => batch.Medicine)
                .ToListAsync();
        }

        public async Task<bool> UpdateMedicineBatchAsync(MedicineBatch medicineBatch)
        {
            _context.MedicineBatches.Update(medicineBatch);
            await _context.SaveChangesAsync();

            return true;
        }
    }
}
