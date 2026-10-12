using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CarePlus.API.Models.CartItem
{
    [Table("CartItems")]
    public class CartItem
    {
        [Key]
        public string CartItemId { get; set; } = null!;

        [Required]
        public string CartId { get; set; } = null!;
        public Cart.Cart Cart { get; set; } = null!;

        [Required]
        public string MedicineId { get; set; } = null!;
        public Medicine.Medicine Medicine { get; set; } = null!;

        [Required]
        public int Quantity { get; set; }

        [Required]
        public double Subtotal { get; set; }

    }
}
