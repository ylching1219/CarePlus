using CarePlus.API.Data;
using CarePlus.API.Models.DeliveryAddress;
using CarePlus.API.Models.Patient;
using CarePlus.API.Models.User;
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

        public async Task<Patient?> GetPatientByUserIdAsync(long id)
        {
            return await _context.Patients.FirstOrDefaultAsync(p => p.UserId == id);
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

        public Task<bool> IdentityCardExistsAsync(string identityCardNumber)
        {
            return _context.Patients.AnyAsync(p => p.IdentityCardNumber == identityCardNumber);
        }

        public async Task<Patient?> GetPatientByIdAsync(string id)
        {
            return await _context.Patients.Include(p => p.User).FirstOrDefaultAsync(p => p.PatientId == id);
        }

        public Task<List<DeliveryAddress>> GetDeliveryAddressesAsync(string patientId)
        {
            return _context.DeliveryAddresses
                .Where(address => address.PatientId == patientId)
                .OrderBy(address => address.Id)
                .ToListAsync();
        }

        public Task<DeliveryAddress?> GetDeliveryAddressAsync(long id, string patientId)
        {
            return _context.DeliveryAddresses.FirstOrDefaultAsync(address => address.Id == id && address.PatientId == patientId);
        }

        public async Task<DeliveryAddress> AddDeliveryAddressAsync(DeliveryAddress address)
        {
            if (address.IsDefault)
            {
                var current = await _context.DeliveryAddresses
                    .Where(row => row.PatientId == address.PatientId && row.IsDefault)
                    .ToListAsync();
                foreach (var row in current)
                {
                    row.IsDefault = false;
                }
            }

            await _context.DeliveryAddresses.AddAsync(address);
            await _context.SaveChangesAsync();
            return address;
        }

        public async Task UpdateDeliveryAddressAsync(DeliveryAddress address)
        {
            _context.DeliveryAddresses.Update(address);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteDeliveryAddressAsync(DeliveryAddress address)
        {
            _context.DeliveryAddresses.Remove(address);
            await _context.SaveChangesAsync();
        }

        public async Task<bool> SetDefaultDeliveryAddressAsync(string patientId, long? id)
        {
            var rows = await _context.DeliveryAddresses.Where(address => address.PatientId == patientId).ToListAsync();
            if (id != null && rows.All(row => row.Id != id))
            {
                return false;
            }

            foreach (var row in rows)
            {
                row.IsDefault = id != null && row.Id == id;
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UpdatePatientAsync(Patient patient)
        {
            _context.Patients.Update(patient);
            await _context.SaveChangesAsync();

            return true;
        }
    }
}
