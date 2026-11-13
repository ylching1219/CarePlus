namespace CarePlus.API.DTO.AuditLogDTO
{
    public class AuditLogRetrieveResponse
    {
        public string LogId { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public string? ActorName { get; set; }
        public string? ActorUsername { get; set; }
        public string? ActorRole { get; set; }
        public string Action { get; set; } = string.Empty;
        public string Module { get; set; } = string.Empty;
        public string AffectedRecord { get; set; } = string.Empty;
        public DateTime DateTime { get; set; }
    }
}
