using CarePlus.API.Extension;
using CarePlus.API.Models.MedicalRecord;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CarePlus.API.Models.Invoice
{
    [Table("Invoices")]
    public class Invoice
    {
        [Key]
        public string InvoiceId { get; set; } = null!;

        [Required]
        public string PatientId { get; set; } = null!;
        public Patient.Patient Patient { get; set; } = null!;

        public string? MedicalRecordId { get; set; }
        public MedRecord? MedicalRecord { get; set; }

        public string? OrderId { get; set; }
        public MedicineOrder.MedicineOrder? MedicineOrder { get; set; }

        public decimal TotalAmount { get; set; }

        public DateTime IssueAt { get; set; } = DateTimeHelper.MalaysiaNow();

        public DateTime? DueDate { get; set; }

        [MaxLength(120)]
        public string? DoctorName { get; set; }

        [MaxLength(80)]
        public string? VisitType { get; set; }

        [MaxLength(40)]
        public string? RecordRef { get; set; }

        public string PaymentStatus { get; set; } = "Pending";


    }
}
