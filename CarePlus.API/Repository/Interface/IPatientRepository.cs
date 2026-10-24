using CarePlus.API.DTO.PatientDTO;
using CarePlus.API.Models.User;
using CarePlus.API.Models.DeliveryAddress;
using CarePlus.API.Models.Patient;
using System.Linq.Expressions;

namespace CarePlus.API.Repository.Interface
{
    public interface IPatientRepository
    {
        Task<Patient?> GetPatientByIdAsync(string id);
        Task<Patient?> GetPatientByUserIdAsync(long id);
        Task<string> CreatePatientAsync(Patient patient);
        Task<bool> UpdatePatientAsync(Patient patient);
        Task<List<Patient>> GetAllPatientsAsync();
        Task<List<Patient>> GetAllPatientsByAsync(Expression<Func<Patient, bool>> predicate);
        Task<bool> IdentityCardExistsAsync(string identityCardNumber);
        Task<List<DeliveryAddress>> GetDeliveryAddressesAsync(string patientId);
        Task<DeliveryAddress?> GetDeliveryAddressAsync(long id, string patientId);
        Task<DeliveryAddress> AddDeliveryAddressAsync(DeliveryAddress address);
        Task UpdateDeliveryAddressAsync(DeliveryAddress address);
        Task DeleteDeliveryAddressAsync(DeliveryAddress address);
        Task<bool> SetDefaultDeliveryAddressAsync(string patientId, long? id);
    }
}
