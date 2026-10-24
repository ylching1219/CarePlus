using System.ComponentModel.DataAnnotations;

namespace CarePlus.API.DTO.StaffDTO
{
    public record StaffUserUpdateRequest
    {
        [Required(ErrorMessage = "Staff Id is required.")]
        public string StaffId { get; init; } = string.Empty;

        [StringLength(100, MinimumLength = 3, ErrorMessage = "Username must be between 3 and 100 characters.")]
        public string? Username { get; init; }

        [StringLength(100, MinimumLength = 3, ErrorMessage = "Name must be between 3 and 100 characters.")]
        public string? Name { get; init; }

        public string? Phone { get; init; }

        public string? Status { get; init; }
    }
}
