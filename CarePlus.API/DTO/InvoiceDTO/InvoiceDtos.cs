namespace CarePlus.API.DTO.InvoiceDTO
{
    public class OutstandingInvoiceResponse
    {
        public string Id { get; set; } = "";
        public string RecordId { get; set; } = "";
        public string IssuedAt { get; set; } = "";
        public string DueAt { get; set; } = "";
        public string Status { get; set; } = "";
        public string DoctorName { get; set; } = "";
        public string VisitType { get; set; } = "";
        public decimal Total { get; set; }
        public int DaysOverdue { get; set; }
        public decimal LateFee { get; set; }
        public decimal AmountDue { get; set; }
    }

    public class InvoicePayRequest
    {
        public string PaymentMethod { get; set; } = "";
    }
}
