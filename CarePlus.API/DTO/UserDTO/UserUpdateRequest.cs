using System.ComponentModel.DataAnnotations;

namespace CarePlus.API.DTO.UserDTO
{
    public record UserUpdateRequest
    {
        [Required(ErrorMessage = "User Id is required")]
        public long Id { get; set; }

        [StringLength(100, MinimumLength = 3, ErrorMessage = "Username must be between 3 and 100 characters.")]
        public string? Username { get; set; }

        [StringLength(100, MinimumLength = 3, ErrorMessage = "Name cannot exceed 100 characters.")]
        public string? Name { get; set; } 

        public string? Phone { get; set; }

        public string? Status { get; set; }
    }
}
