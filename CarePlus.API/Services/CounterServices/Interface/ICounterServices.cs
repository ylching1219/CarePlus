namespace CarePlus.API.Services.CounterServices.Interface
{
    public interface ICounterServices
    {
        Task<string> GenerateNextIdAsync(string key);
    }
}
