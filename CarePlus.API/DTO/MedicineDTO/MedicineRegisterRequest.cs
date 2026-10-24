using System.ComponentModel.DataAnnotations;

namespace CarePlus.API.DTO.MedicineDTO
{
    public record MedicineRegisterRequest
    {
        [Required(ErrorMessage = "Medicine name is required.")]
        [StringLength(100)]
        public string MedicineName { get; init; } = string.Empty;

        [Required(ErrorMessage = "Description is required.")]
        [StringLength(500)]
        public string Description { get; init; } = string.Empty;

        [Range(typeof(decimal), "0.01", "9999999999999999.99", ErrorMessage = "Price must be greater than 0.")]
        public decimal Price { get; init; }

    }
}
