using CarePlus.API.DTO.MedicalRecordDTO;

namespace CarePlus.API.Services.MedicalRecordServices.Interface;

public interface IMedicalRecordServices
{
    Task<ApiResponse<List<MedicalRecordResponse>>> RetrieveForDoctorAsync(long userId);
    Task<ApiResponse<MedicalRecordResponse>> CreateAsync(long userId, MedicalRecordCreateRequest request);
    Task<ApiResponse<MedicalRecordResponse>> UpdateAsync(long userId, MedicalRecordUpdateRequest request);
}
