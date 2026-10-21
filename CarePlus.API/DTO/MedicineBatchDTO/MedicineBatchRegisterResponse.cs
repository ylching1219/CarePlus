namespace CarePlus.API.DTO.MedicineBatchDTO
{
    public class MedicineBatchRegisterResponse
    {
        public string BatchId { get; set; } = string.Empty;
        public string MedicineId { get; set; } = string.Empty;
        public int StockQuantity { get; set; }
        public DateTime ExpiryDate { get; set; }
    }
}
