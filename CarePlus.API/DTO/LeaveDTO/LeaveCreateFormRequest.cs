using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace CarePlus.API.DTO.LeaveDTO
{
    public record LeaveCreateFormRequest
    {
        [Required(ErrorMessage = "Staff Id is required.")]
        public string StaffId { get; init; } = string.Empty;

        [Required(ErrorMessage = "Start date is required.")]
        public DateTime StartDate { get; init; }

        [Required(ErrorMessage = "End date is required.")]
        public DateTime EndDate { get; init; }

        [Required(ErrorMessage = "Reason is required.")]
        [StringLength(500, ErrorMessage = "Reason cannot exceed 500 characters.")]
        public string Reason { get; init; } = string.Empty;

        [Required(ErrorMessage = "Support document is required.")]
        public IFormFile? SupportDocument { get; init; }
    }
}
