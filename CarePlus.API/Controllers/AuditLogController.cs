using CarePlus.API.Services.AuditLogServices.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text;

namespace CarePlus.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")]
    public class AuditLogController : ControllerBase
    {
        private readonly IAuditLogServices _auditLogServices;

        public AuditLogController(IAuditLogServices auditLogServices)
        {
            _auditLogServices = auditLogServices;
        }

        [HttpGet("Retrieve")]
        public async Task<IActionResult> Retrieve(
            [FromQuery] string? search,
            [FromQuery] string? action,
            [FromQuery] string? module,
            [FromQuery] DateTime? from,
            [FromQuery] DateTime? to,
            [FromQuery] int pageSize = 100)
        {
            return Ok(await _auditLogServices.RetrieveAsync(
                search, action, module, from, to, pageSize));
        }

        [HttpGet("RetrieveById/{logId}")]
        public async Task<IActionResult> RetrieveById(string logId)
        {
            return Ok(await _auditLogServices.RetrieveByIdAsync(logId));
        }

        [HttpGet("Export")]
        public async Task<IActionResult> Export(
            [FromQuery] string? search,
            [FromQuery] string? action,
            [FromQuery] string? module,
            [FromQuery] DateTime? from,
            [FromQuery] DateTime? to)
        {
            var logs = await _auditLogServices.ExportAsync(
                search, action, module, from, to);
            var csv = new StringBuilder();
            csv.AppendLine("LogId,UserId,Action,Module,AffectedRecord,DateTime");

            foreach (var log in logs)
            {
                csv.AppendLine(string.Join(",", new[]
                {
                    Escape(log.LogId),
                    Escape(log.UserId),
                    Escape(log.Action),
                    Escape(log.Module),
                    Escape(log.AffectedRecord),
                    Escape(log.DateTime.ToString("O"))
                }));
            }

            return File(
                Encoding.UTF8.GetBytes(csv.ToString()),
                "text/csv",
                $"audit-log-{DateTime.UtcNow:yyyyMMddHHmmss}.csv");
        }

        private static string Escape(string value)
        {
            return $"\"{value.Replace("\"", "\"\"")}\"";
        }
    }
}
