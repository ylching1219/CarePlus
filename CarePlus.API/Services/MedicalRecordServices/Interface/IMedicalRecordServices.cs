using CarePlus.API.DTO.MedicalRecordDTO;

namespace CarePlus.API.Services.MedicalRecordServices.Interface;

public interface IMedicalRecordServices
{
    Task<ApiResponse<List<MedicalRecordResponse>>> RetrieveAsync(long userId, string role);
    Task<ApiResponse<MedicalRecordResponse>> CreateAsync(long userId, string role, MedicalRecordCreateRequest request);
    Task<ApiResponse<MedicalRecordResponse>> UpdateAsync(long userId, string role, MedicalRecordUpdateRequest request);
}
