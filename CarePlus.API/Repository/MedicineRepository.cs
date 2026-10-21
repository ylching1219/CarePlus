using CarePlus.API.Data;
using CarePlus.API.Models.Medicine;
using CarePlus.API.Repository.Interface;
using Microsoft.EntityFrameworkCore;

namespace CarePlus.API.Repository
{
    public class MedicineRepository : IMedicineRepository
    {
        private readonly AppDbContext _context;

        public MedicineRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<string> CreateMedicineAsync(Medicine medicine)
        {
            await _context.Medicines.AddAsync(medicine);
            await _context.SaveChangesAsync();

            return medicine.MedicineId;
        }

        public async Task<Medicine?> GetMedicineByIdAsync(string medicineId)
        {
            return await _context.Medicines
                .FirstOrDefaultAsync(m => m.MedicineId == medicineId);
        }

        public async Task<List<Medicine>> GetAllMedicinesAsync()
        {
            return await _context.Medicines.ToListAsync();
        }

        public async Task<bool> UpdateMedicineAsync(Medicine medicine)
        {
            _context.Medicines.Update(medicine);
            await _context.SaveChangesAsync();

            return true;
        }
    }
}
