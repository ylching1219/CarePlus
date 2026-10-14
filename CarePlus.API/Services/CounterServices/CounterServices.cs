using CarePlus.API.Repository.Interface;
using CarePlus.API.Services.CounterServices.Interface;

namespace CarePlus.API.Services.CounterServices
{
    public class CounterServices : ICounterServices
    {
        private readonly ICounterRepository _counterRepository;

        public CounterServices(ICounterRepository counterRepository)
        {
            _counterRepository = counterRepository;
        }

        public async Task<string> GenerateNextIdAsync(string key)
        {
            var counter = await _counterRepository.GetCounterByAsync(
                c => c.Key == key);

            if (counter == null)
            {
                throw new InvalidOperationException(
                    $"Counter with key '{key}' was not found.");
            }

            if (!counter.IsActive)
            {
                throw new InvalidOperationException(
                    $"Counter with key '{key}' is inactive.");
            }

            counter.Value += counter.Step;

            await _counterRepository.UpdateCounterAsync(counter);

            return counter.FormattedValue;
        }
    }
}
