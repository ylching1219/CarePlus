using CarePlus.API.DTO.AnnouncementDTO;

namespace CarePlus.API.Services.AnnouncementServices.Interface;

public interface IAnnouncementServices
{
    Task<ApiResponse<AnnouncementResponse>> CreateAsync(AnnouncementCreateRequest request);
}
