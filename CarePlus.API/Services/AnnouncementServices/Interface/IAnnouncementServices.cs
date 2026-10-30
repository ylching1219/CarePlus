using CarePlus.API.DTO;
using CarePlus.API.DTO.AnnouncementDTO;

namespace CarePlus.API.Services.AnnouncementServices.Interface;

public interface IAnnouncementServices
{
    Task<ApiResponse<List<AnnouncementResponse>>> ListPublishedAsync();
}
