using CarePlus.API.Data;
using CarePlus.API.Models.Common;
using CarePlus.API.Repository.Interface;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace CarePlus.API.Repository
{
    public class CounterRepository : ICounterRepository
    {
        private readonly AppDbContext _context;

        public CounterRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<long> CreateCounterAsync(Counter counter)
        {
            await _context.Counters.AddAsync(counter);
            await _context.SaveChangesAsync();

            return counter.Id;
        }

        public async Task<List<Counter>> GetAllCountersAsync()
        {
            return await _context.Counters.ToListAsync();
        }

        public async Task<List<Counter>> GetAllCountersByAsync(Expression<Func<Counter, bool>> predicate)
        {
            return await _context.Counters.Where(predicate).ToListAsync();
        }

        public async Task<Counter?> GetCounterByAsync(Expression<Func<Counter, bool>> predicate)
        {
            return await _context.Counters.FirstOrDefaultAsync(predicate);
        }

        public async Task<Counter?> GetCounterByIdAsync(long id)
        {
            return await this.GetCounterByAsync(c => c.Id == id);
        }

        public async Task<bool> UpdateCounterAsync(Counter counter)
        {
            counter.Version++;
            _context.Counters.Update(counter);
            await _context.SaveChangesAsync();

            return true;
        }
    }
}
