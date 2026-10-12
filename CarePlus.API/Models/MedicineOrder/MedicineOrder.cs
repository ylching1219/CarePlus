using CarePlus.API.Extension;
using CarePlus.API.Models.User.Patient;
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
        public Patient Patient { get; set; } = null!;

        public decimal TotalAmount { get; set; } = 0;
        public DateTime OrderDate { get; set; } = DateTimeHelper.MalaysiaNow();
        public string Status { get; set; } = "Pending";

    }
}
