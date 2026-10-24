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

        public string? MedicineId { get; set; }
        public Medicine.Medicine? Medicine { get; set; }

        [Required]
        [MaxLength(120)]
        public string ItemName { get; set; } = "";

        [MaxLength(200)]
        public string? Detail { get; set; }

        [Required]
        public decimal UnitPrice { get; set; }

        [Required]
        public int Quantity { get; set; }

        [Required]
        public decimal SubTotal { get; set; }

    }
}
