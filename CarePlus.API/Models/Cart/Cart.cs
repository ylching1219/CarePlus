using CarePlus.API.Models.User.Patient;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CarePlus.API.Models.Cart
{
    [Table("ShoppingCarts")]
    public class Cart
    {
        [Key]
        public string CartId { get; set; } = null!;

        [Required]
        public string PatientId { get; set; } = null!;
        public Patient Patient { get; set; } = null!;

        public decimal TotalAmount { get; set; } = 0;

    }
}
