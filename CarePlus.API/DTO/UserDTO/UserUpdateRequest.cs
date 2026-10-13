using System.ComponentModel.DataAnnotations;

namespace CarePlus.API.DTO.UserDTO
{
    public record UserUpdateRequest
    {
        [Required(ErrorMessage = "User Id is required")]
        public long Id { get; set; }

        [StringLength(100, MinimumLength = 3, ErrorMessage = "Name cannot exceed 100 characters.")]
        public string? name { get; set; } 

        public string? phone { get; set; }

        public string status { get; set; } = string.Empty;
    }
}
