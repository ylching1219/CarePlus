namespace CarePlus.API.Models.Common
{
    public class AuditTrail
    {
        public DateTime CreateDate { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime? UpdateDate { get; set; }
        public string? UpdatedBy { get; set; }
        public DateTime? DeleteDate { get; set; }
        public string? DeletedBy { get; set; }  
    }
}
