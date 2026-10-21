using System.ComponentModel.DataAnnotations;

namespace CarePlus.API.DTO.MedicineBatchDTO
{
    public record MedicineBatchUpdateRequest
    {
        [Required(ErrorMessage = "Batch Id is required.")]
        public string BatchId { get; init; } = string.Empty;

        [Range(0, int.MaxValue, ErrorMessage = "Stock quantity cannot be negative.")]
        public int? StockQuantity { get; init; }

        public DateTime? ExpiryDate { get; init; }
    }
}
