using CarePlus.API.Extension;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CarePlus.API.Models.Payment
{
    [Table("Payments")]
    public class Payment
    {
        [Key]
        public string PaymentId { get; set; } = null!;

        [Required]
        public string InvoiceId { get; set; } = null!;
        public Invoice.Invoice Invoice { get; set; } = null!;

        public decimal TotalAmount { get; set; }

        [Required]
        public string PaymentMethod { get; set; } = null!;

        public DateTime TransactionDate { get; set; } = DateTimeHelper.MalaysiaNow();

        public string Status { get; set; } = "Pending";
    }
}
