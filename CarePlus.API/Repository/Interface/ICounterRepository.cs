using CarePlus.API.Models.Common;
using System.Linq.Expressions;

namespace CarePlus.API.Repository.Interface
{
    public interface ICounterRepository
    {
        Task<long> CreateCounterAsync(Counter counter);
        Task<bool> UpdateCounterAsync(Counter counter);
        Task<Counter?> GetCounterByIdAsync(long id);
        Task<Counter?> GetCounterByAsync(Expression<Func<Counter, bool>> predicate);
        Task<List<Counter>> GetAllCountersAsync();
        Task<List<Counter>> GetAllCountersByAsync(Expression<Func<Counter, bool>> predicate);
    }
}
