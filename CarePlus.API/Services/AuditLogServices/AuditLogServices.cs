using CarePlus.API.Data;
using CarePlus.API.DTO.AuditLogDTO;
using CarePlus.API.Services.AuditLogServices.Interface;
using Microsoft.EntityFrameworkCore;

namespace CarePlus.API.Services.AuditLogServices
{
    public class AuditLogServices : IAuditLogServices
    {
        private readonly AppDbContext _db;

        public AuditLogServices(AppDbContext db)
        {
            _db = db;
        }

        public async Task<ApiResponse<object>> RetrieveAsync(
            string? search,
            string? action,
            string? module,
            DateTime? from,
            DateTime? to,
            int pageSize = 100)
        {
            var query = BuildQuery(search, action, module, from, to);
            var logs = await query
                .OrderByDescending(log => log.DateTime)
                .Take(Math.Clamp(pageSize, 1, 500))
                .Select(ToResponse())
                .ToListAsync();

            return ApiResponse<object>.Ok(logs);
        }

        public async Task<ApiResponse<object>> RetrieveByIdAsync(string logId)
        {
            var log = await _db.AuditLogs
                .AsNoTracking()
                .Where(item => item.LogId == logId)
                .Select(ToResponse())
                .FirstOrDefaultAsync();

            return log == null
                ? ApiResponse<object>.Fail("Audit log not found.", 404)
                : ApiResponse<object>.Ok(log);
        }

        public Task<List<AuditLogRetrieveResponse>> ExportAsync(
            string? search,
            string? action,
            string? module,
            DateTime? from,
            DateTime? to)
        {
            return BuildQuery(search, action, module, from, to)
                .OrderByDescending(log => log.DateTime)
                .Select(ToResponse())
                .ToListAsync();
        }

        private IQueryable<Models.Common.AuditLog> BuildQuery(
            string? search,
            string? action,
            string? module,
            DateTime? from,
            DateTime? to)
        {
            var query = _db.AuditLogs.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var value = search.Trim();
                query = query.Where(log =>
                    log.LogId.Contains(value)
                    || log.UserId.Contains(value)
                    || log.Module.Contains(value)
                    || log.Action.Contains(value)
                    || log.AffectedRecord.Contains(value));
            }

            if (!string.IsNullOrWhiteSpace(action))
            {
                query = query.Where(log => log.Action == action.Trim());
            }

            if (!string.IsNullOrWhiteSpace(module))
            {
                query = query.Where(log => log.Module == module.Trim());
            }

            if (from.HasValue)
            {
                query = query.Where(log => log.DateTime >= from.Value);
            }

            if (to.HasValue)
            {
                query = query.Where(log => log.DateTime <= to.Value);
            }

            return query;
        }

        private static System.Linq.Expressions.Expression<
            Func<Models.Common.AuditLog, AuditLogRetrieveResponse>> ToResponse()
        {
            return log => new AuditLogRetrieveResponse
            {
                LogId = log.LogId,
                UserId = log.UserId,
                Action = log.Action,
                Module = log.Module,
                AffectedRecord = log.AffectedRecord,
                DateTime = log.DateTime
            };
        }
    }
}
