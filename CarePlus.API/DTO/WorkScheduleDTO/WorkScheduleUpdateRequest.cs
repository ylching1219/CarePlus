using System.ComponentModel.DataAnnotations;

namespace CarePlus.API.DTO.WorkScheduleDTO
{
    public record WorkScheduleUpdateRequest
    {
        [Required(ErrorMessage = "Schedule Id is required.")]
        public string ScheduleId { get; init; } = string.Empty;

        public DateOnly? WorkingDate { get; init; }

        [StringLength(20)]
        public string? Shift { get; init; }

        [StringLength(20)]
        public string? Status { get; init; }
    }
}
