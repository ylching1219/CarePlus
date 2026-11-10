using CarePlus.API.DTO.ClinicRoomDTO;

namespace CarePlus.API.Services.ClinicRoomServices.Interface;

public interface IClinicRoomServices
{
    Task<ApiResponse<List<ClinicRoomResponse>>> RetrieveAllAsync();
    Task<ApiResponse<ClinicRoomResponse>> CreateAsync(ClinicRoomCreateRequest request);
    Task<ApiResponse<ClinicRoomResponse>> UpdateAsync(ClinicRoomUpdateRequest request);
    Task<ApiResponse<List<RoomAssignmentResponse>>> RetrieveAssignmentsAsync(
        DateOnly? date,
        string? scheduleId);
    Task<ApiResponse<RoomAssignmentResponse>> AssignAsync(RoomAssignmentCreateRequest request);
    Task<ApiResponse<object>> RemoveAssignmentAsync(string assignmentId);
}
