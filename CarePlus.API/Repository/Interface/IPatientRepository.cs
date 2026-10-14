using CarePlus.API.DTO.PatientDTO;
using CarePlus.API.Models.User;
using CarePlus.API.Models.Patient;
using System.Linq.Expressions;

namespace CarePlus.API.Repository.Interface
{
    public interface IPatientRepository
    {
        Task<Patient?> GetPatientByIdAsync(string id);
        Task<string> CreatePatientAsync(Patient patient);
        Task<bool> UpdatePatientAsync(Patient patient);
        Task<List<Patient>> GetAllPatientsAsync();
        Task<List<Patient>> GetAllPatientsByAsync(Expression<Func<Patient, bool>> predicate);
    }
}
