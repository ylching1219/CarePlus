using CarePlus.API.Extension;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CarePlus.API.Models.MedicineOrder
{
    [Table("MedicineOrders")]
    public class MedicineOrder
    {
        [Key]
        public string OrderId { get; set; } = null!;

        [Required]
        public string PatientId { get; set; } = null!;
        public Patient.Patient Patient { get; set; } = null!;

        public decimal TotalAmount { get; set; } = 0;

        public decimal DeliveryFee { get; set; }

        public DateTime OrderDate { get; set; } = DateTimeHelper.MalaysiaNow();

        public string Status { get; set; } = "Pending";

        [MaxLength(20)]
        public string Fulfilment { get; set; } = "pickup";

        [MaxLength(80)]
        public string PaymentMethod { get; set; } = "";

        public long? DeliveryAddressId { get; set; }

        public DeliveryAddress.DeliveryAddress? ChosenAddress { get; set; }

        [MaxLength(300)]
        public string? DeliveryAddress { get; set; }

        public double? Latitude { get; set; }

        public double? Longitude { get; set; }

    }
}
