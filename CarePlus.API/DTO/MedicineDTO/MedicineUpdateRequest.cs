using System.ComponentModel.DataAnnotations;

namespace CarePlus.API.DTO.MedicineDTO
{
    public record MedicineUpdateRequest
    {
        [Required(ErrorMessage = "Medicine Id is required.")]
        public string MedicineId { get; init; } = string.Empty;

        [StringLength(100)]
        public string? MedicineName { get; init; }

        [StringLength(100)]
        public string? NameZh { get; init; }

        [StringLength(500)]
        public string? Description { get; init; }

        [StringLength(500)]
        public string? DescriptionZh { get; init; }

        [StringLength(160)]
        public string? Method { get; init; }

        [StringLength(160)]
        public string? MethodZh { get; init; }

        [Range(typeof(decimal), "0.01", "9999999999999999.99", ErrorMessage = "Price must be greater than 0.")]
        public decimal? Price { get; init; }

        [StringLength(20)]
        public string? Status { get; init; }
    }
}
