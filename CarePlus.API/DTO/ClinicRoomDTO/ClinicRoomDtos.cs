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
