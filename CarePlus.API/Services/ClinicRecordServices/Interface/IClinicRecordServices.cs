using CarePlus.API.DTO.ClinicRecordDTO;

namespace CarePlus.API.Services.ClinicRecordServices.Interface
{
    public interface IClinicRecordServices
    {
        Task<ApiResponse<List<ClinicRecordResponse>>> ListForPatientAsync(long userId, string? locale);
    }
}
