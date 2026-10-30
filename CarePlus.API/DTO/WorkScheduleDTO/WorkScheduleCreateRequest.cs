using System.ComponentModel.DataAnnotations;

namespace CarePlus.API.DTO.WorkScheduleDTO
{
    public record WorkScheduleCreateRequest
    {
        [Required(ErrorMessage = "Staff Id is required.")]
        public string StaffId { get; init; } = string.Empty;

        public DateOnly WorkingDate { get; init; }

        [StringLength(20)]
        [Required(ErrorMessage = "Shift is required.")]
        public string Shift { get; init; } = string.Empty;
    }
}
