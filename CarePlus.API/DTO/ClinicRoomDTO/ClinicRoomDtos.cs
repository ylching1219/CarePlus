using System.ComponentModel.DataAnnotations;

namespace CarePlus.API.DTO.ClinicRoomDTO;

public class ClinicRoomResponse
{
    public string RoomId { get; set; } = string.Empty;
    public string RoomName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}

public class ClinicRoomCreateRequest
{
    [Required]
    [StringLength(100, MinimumLength = 2)]
    public string RoomName { get; set; } = string.Empty;

    public string? Status { get; set; }
}

public class ClinicRoomUpdateRequest
{
    [Required]
    public string RoomId { get; set; } = string.Empty;

    [Required]
    [StringLength(100, MinimumLength = 2)]
    public string RoomName { get; set; } = string.Empty;

    [Required]
    public string Status { get; set; } = string.Empty;
}

public class RoomAssignmentResponse
{
    public string AssignmentId { get; set; } = string.Empty;
    public string ScheduleId { get; set; } = string.Empty;
    public string RoomId { get; set; } = string.Empty;
    public string RoomName { get; set; } = string.Empty;
    public string DoctorId { get; set; } = string.Empty;
    public string DoctorName { get; set; } = string.Empty;
    public DateOnly AssignmentDate { get; set; }
    public string Shift { get; set; } = string.Empty;
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
}

public class RoomAssignmentCreateRequest
{
    [Required]
    public string ScheduleId { get; set; } = string.Empty;

    [Required]
    public string RoomId { get; set; } = string.Empty;
}
