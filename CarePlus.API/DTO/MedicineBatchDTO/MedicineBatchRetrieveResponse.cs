namespace CarePlus.API.DTO.MedicineBatchDTO
{
    public class MedicineBatchRetrieveResponse
    {
        public string BatchId { get; set; } = string.Empty;
        public string MedicineId { get; set; } = string.Empty;
        public string MedicineName { get; set; } = string.Empty;
        public int StockQuantity { get; set; }
        public DateTime ExpiryDate { get; set; }
    }
}
