using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CarePlus.API.Models.Room
{
    [Table("ClinicRooms")]
    public class ClinicRoom
    {
        [Key]
        public string RoomId { get; set; } = null!;

        [Required]
        public string RoomName { get; set; } = null!;

        public string Status { get; set; } = "Available";
    }
}
