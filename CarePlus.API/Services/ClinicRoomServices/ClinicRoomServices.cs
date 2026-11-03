using CarePlus.API.Data;
using CarePlus.API.DTO.ClinicRoomDTO;
using CarePlus.API.Models.Room;
using CarePlus.API.Services.ClinicRoomServices.Interface;
using Microsoft.EntityFrameworkCore;

namespace CarePlus.API.Services.ClinicRoomServices;

public class ClinicRoomServices : IClinicRoomServices
{
    private static readonly HashSet<string> ValidStatuses = new(StringComparer.OrdinalIgnoreCase)
    {
        "Available",
        "Occupied",
        "Maintenance",
        "Inactive",
    };

    private readonly AppDbContext _db;

    public ClinicRoomServices(AppDbContext db)
    {
        _db = db;
    }

    public async Task<ApiResponse<List<ClinicRoomResponse>>> RetrieveAllAsync()
    {
        var rooms = await _db.ClinicRooms
            .AsNoTracking()
            .OrderBy(room => room.RoomName)
            .Select(room => ToResponse(room))
            .ToListAsync();

        return ApiResponse<List<ClinicRoomResponse>>.Ok(rooms);
    }

    public async Task<ApiResponse<ClinicRoomResponse>> CreateAsync(ClinicRoomCreateRequest request)
    {
        var roomName = request.RoomName.Trim();
        if (roomName.Length < 2)
        {
            return ApiResponse<ClinicRoomResponse>.Fail("Room name is required.", 400);
        }

        var status = NormalizeStatus(request.Status);
        if (status == null)
        {
            return ApiResponse<ClinicRoomResponse>.Fail(
                "Status must be Available, Occupied, Maintenance, or Inactive.", 400);
        }

        if (await _db.ClinicRooms.AnyAsync(room => room.RoomName == roomName))
        {
            return ApiResponse<ClinicRoomResponse>.Fail("A room with this name already exists.", 409);
        }

        var room = new ClinicRoom
        {
            RoomId = $"RM-{Guid.NewGuid():N}"[..11].ToUpperInvariant(),
            RoomName = roomName,
            Status = status,
        };

        await _db.ClinicRooms.AddAsync(room);
        await _db.SaveChangesAsync();

        return ApiResponse<ClinicRoomResponse>.Ok(ToResponse(room));
    }

    public async Task<ApiResponse<ClinicRoomResponse>> UpdateAsync(ClinicRoomUpdateRequest request)
    {
        var room = await _db.ClinicRooms
            .FirstOrDefaultAsync(item => item.RoomId == request.RoomId.Trim());

        if (room == null)
        {
            return ApiResponse<ClinicRoomResponse>.Fail("Clinic room not found.", 404);
        }

        var roomName = request.RoomName.Trim();
        var status = NormalizeStatus(request.Status);
        if (roomName.Length < 2)
        {
            return ApiResponse<ClinicRoomResponse>.Fail("Room name is required.", 400);
        }

        if (status == null)
        {
            return ApiResponse<ClinicRoomResponse>.Fail(
                "Status must be Available, Occupied, Maintenance, or Inactive.", 400);
        }

        if (await _db.ClinicRooms.AnyAsync(item =>
                item.RoomId != room.RoomId && item.RoomName == roomName))
        {
            return ApiResponse<ClinicRoomResponse>.Fail("A room with this name already exists.", 409);
        }

        room.RoomName = roomName;
        room.Status = status;
        await _db.SaveChangesAsync();

        return ApiResponse<ClinicRoomResponse>.Ok(ToResponse(room));
    }

    private static ClinicRoomResponse ToResponse(ClinicRoom room) => new()
    {
        RoomId = room.RoomId,
        RoomName = room.RoomName,
        Status = room.Status,
    };

    private static string? NormalizeStatus(string? status)
    {
        var value = string.IsNullOrWhiteSpace(status) ? "Available" : status.Trim();
        return ValidStatuses.FirstOrDefault(item =>
            item.Equals(value, StringComparison.OrdinalIgnoreCase));
    }
}
