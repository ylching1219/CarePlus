using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CarePlus.API.Models.OrderItem
{
    [Table("OrderItems")]
    public class OrderItem
    {
        [Key]
        public string OrderItemId { get; set; } = null!;

        [Required]
        public string OrderId { get; set; } = null!;
        public MedicineOrder.MedicineOrder Order { get; set; } = null!;

        [Required]
        public string MedicineId { get; set; } = null!;
        public Medicine.Medicine Medicine { get; set; } = null!;

        [Required]
        public int Quantity { get; set; }

        [Required]
        public decimal SubTotal { get; set; }

    }
}
