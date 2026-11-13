using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CarePlus.API.Models.Payment
{
    [Table("paymentCard")]
    public class PaymentCard
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        [Required]
        public long UserId { get; set; }

        public CarePlus.API.Models.User.User User { get; set; } = null!;

        [Required]
        [MaxLength(20)]
        public string Brand { get; set; } = string.Empty;

        [Required]
        [MaxLength(4)]
        public string Last4 { get; set; } = string.Empty;

        [Required]
        [MaxLength(5)]
        public string Expiry { get; set; } = string.Empty;

        [Required]
        [MaxLength(26)]
        public string HolderName { get; set; } = string.Empty;

        [Required]
        [MaxLength(300)]
        public string NumberHash { get; set; } = string.Empty;

        [Required]
        [MaxLength(300)]
        public string CvvHash { get; set; } = string.Empty;

        public bool IsDefault { get; set; }
    }
}
