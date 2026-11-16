using System.ComponentModel.DataAnnotations;

namespace CarePlus.API.DTO.StaffDTO
{
    public record AdminUpdateRequest
    {
        [Required(ErrorMessage = "Staff Id is required.")]
        public string StaffId { get; init; } = string.Empty;
    }
}
