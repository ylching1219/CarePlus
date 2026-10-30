using System.ComponentModel.DataAnnotations;

namespace CarePlus.API.DTO.LeaveDTO
{
    public record LeaveStatusUpdateRequest
    {
        [Required(ErrorMessage = "Leave Id is required.")]
        public string LeaveId { get; init; } = string.Empty;

        [Required(ErrorMessage = "Status is required.")]
        [StringLength(20)]
        public string Status { get; init; } = string.Empty;
    }
}
