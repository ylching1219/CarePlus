using CarePlus.API.Data;
using CarePlus.API.Models.Patient;
using CarePlus.API.Repository.Interface;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace CarePlus.API.Repository
{
    public class PatientRepository : IPatientRepository
    {
        private readonly AppDbContext _context;

        public PatientRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<string> CreatePatientAsync(Patient patient)
        {
            await _context.Patients.AddAsync(patient);
            await _context.SaveChangesAsync();

            return patient.PatientId;
        }

        public async Task<List<Patient>> GetAllPatientsAsync()
        {
            List<Patient> patients = new List<Patient>();
            patients = await _context.Patients.Include(p=>p.User).ToListAsync();

            return patients;
        }

        public async Task<List<Patient>> GetAllPatientsByAsync(Expression<Func<Patient, bool>> predicate)
        {
            return await _context.Patients.Include(p => p.User).Where(predicate).ToListAsync();
        }

        public async Task<Patient?> GetPatientByIdAsync(string id)
        {
            return await _context.Patients.Include(p => p.User).FirstOrDefaultAsync(p => p.PatientId == id);
        }

        public async Task<bool> UpdatePatientAsync(Patient patient)
        {
            _context.Patients.Update(patient);
            await _context.SaveChangesAsync();

            return true;
        }
    }
}
