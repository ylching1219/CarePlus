using CarePlus.API.Models.Room;
using CarePlus.API.Models.RoomAssg;
using CarePlus.API.Models.WorkSch;

namespace CarePlus.API.Repository.Interface;

public interface IClinicRoomRepository
{
    Task<List<ClinicRoom>> GetAllRoomsAsync();
    Task<ClinicRoom?> GetRoomByIdAsync(string roomId);
    Task<bool> RoomNameExistsAsync(string roomName, string? excludedRoomId = null);
    Task AddRoomAsync(ClinicRoom room);
    Task<List<RoomAssignment>> GetRoomAssignmentsAsync(DateOnly? date, string? scheduleId);
    Task<List<RoomAssignment>> GetAssignmentsByRoomIdAsync(string roomId, bool trackChanges = false);
    Task<List<RoomAssignment>> GetAssignmentsExceptRoomAsync(string roomId);
    Task<List<RoomAssignment>> GetAssignmentsForRoomReassignmentAsync(string roomId);
    Task<RoomAssignment?> GetAssignmentByScheduleIdAsync(string scheduleId, bool trackChanges = false);
    Task<List<RoomAssignment>> GetRoomAssignmentsByScheduleIdsAsync(IEnumerable<string> scheduleIds);
    Task<RoomAssignment?> GetAssignmentByIdAsync(string assignmentId);
    Task<WorkSchedule?> GetScheduleWithStaffAsync(string scheduleId);
    Task<List<ClinicRoom>> GetAvailableRoomsExceptAsync(string roomId);
    Task AddAssignmentAsync(RoomAssignment assignment);
    void RemoveAssignment(RoomAssignment assignment);
    Task SaveChangesAsync();
}
