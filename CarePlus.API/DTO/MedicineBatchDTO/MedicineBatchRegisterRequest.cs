using System.ComponentModel.DataAnnotations;

namespace CarePlus.API.DTO.MedicineBatchDTO
{
    public record MedicineBatchRegisterRequest
    {
        [Required(ErrorMessage = "Medicine Id is required.")]
        public string MedicineId { get; init; } = string.Empty;

        [Range(0, int.MaxValue, ErrorMessage = "Stock quantity cannot be negative.")]
        public int StockQuantity { get; init; }

        [Required(ErrorMessage = "Expiry date is required.")]
        public DateTime ExpiryDate { get; init; }
    }
}
