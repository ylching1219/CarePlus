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

        public DateTime? DueDate { get; set; }

        public string Status { get; set; } = "Pending";

        public const decimal LateFeePerDay = 10m;

        public int OverdueDays(DateTime asOf)
        {
            if (DueDate == null)
            {
                return 0;
            }

            if (Status.Equals("Paid", StringComparison.OrdinalIgnoreCase) || Status.Equals("Completed", StringComparison.OrdinalIgnoreCase))
            {
                return 0;
            }

            var days = (asOf.Date - DueDate.Value.Date).Days;
            return days > 0 ? days : 0;
        }

        public decimal LateCharge(DateTime asOf)
        {
            return OverdueDays(asOf) * LateFeePerDay;
        }
    }
}
