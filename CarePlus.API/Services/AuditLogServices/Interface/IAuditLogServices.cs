using CarePlus.API.DTO.AuditLogDTO;

namespace CarePlus.API.Services.AuditLogServices.Interface
{
    public interface IAuditLogServices
    {
        Task<ApiResponse<object>> RetrieveAsync(
            string? search,
            string? action,
            string? module,
            DateTime? from,
            DateTime? to,
            int pageSize = 100);

        Task<ApiResponse<object>> RetrieveByIdAsync(string logId);

        Task<List<AuditLogRetrieveResponse>> ExportAsync(
            string? search,
            string? action,
            string? module,
            DateTime? from,
            DateTime? to);
    }
}
