using CarePlus.API.DTO.PatientDTO;
using CarePlus.API.Models.User;
using CarePlus.API.Models.Patient;
using System.Linq.Expressions;

namespace CarePlus.API.Repository.Interface
{
    public interface IPatientRepository
    {
        Task<User?> GetPatientByIdAsync(string id);
        Task<long> CreatePatientAsync(Patient patient);
        Task<bool> UpdatePatientAsync(Patient patient);
        Task<List<User>> GetAllPatientsAsync();
        Task<List<User>> GetAllPatientsByAsync(Expression<Func<User, bool>> predicate);
    }
}
