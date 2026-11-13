using CarePlus.API.Models.User.Staff.Doctor;

namespace CarePlus.API.Repository.Interface
{
    public interface IDoctorRepository
    {
        Task<string> CreateDoctorAsync(Doctor doctor);
        Task<Doctor?> GetDoctorByIdAsync(string staffId);
        Task<List<Doctor>> GetAllDoctorsAsync();
        Task<bool> UpdateDoctorAsync(Doctor doctor);
    }
}
