using CarePlus.API.DTO;
using CarePlus.API.DTO.FamilyDTO;

namespace CarePlus.API.Services.FamilyServices.Interface;

public interface IFamilyServices
{
    Task<ApiResponse<FamilyOverviewResponse>> GetOverviewAsync(string patientId);
    Task<ApiResponse<FamilyOverviewResponse>> CreateAsync(string patientId, FamilyCreateRequest request);
    Task<ApiResponse<FamilyOverviewResponse>> RenameAsync(string patientId, FamilyRenameRequest request);
    Task<ApiResponse<FamilyOverviewResponse>> InviteAsync(string patientId, FamilyInviteRequest request);
    Task<ApiResponse<FamilyOverviewResponse>> AcceptInviteAsync(string patientId, long familyMemberId);
    Task<ApiResponse<FamilyOverviewResponse>> DeclineInviteAsync(string patientId, long familyMemberId);
    Task<ApiResponse<FamilyOverviewResponse>> CancelInviteAsync(string patientId, long familyMemberId);
    Task<ApiResponse<FamilyOverviewResponse>> UpdateAccessAsync(string patientId, long familyMemberId, FamilyAccessUpdateRequest request);
    Task<ApiResponse<FamilyOverviewResponse>> RenameSelfAsync(string patientId, FamilyMemberDisplayNameRequest request);
    Task<ApiResponse<FamilyOverviewResponse>> RemoveMemberAsync(string patientId, long familyMemberId);
    Task<ApiResponse<FamilyOverviewResponse>> LeaveAsync(string patientId, FamilyLeaveRequest request);
    Task<ApiResponse<FamilyOverviewResponse>> DeleteAsync(string patientId);
}
