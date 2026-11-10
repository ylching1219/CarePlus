using System.ComponentModel.DataAnnotations;

namespace CarePlus.API.DTO.MedicalRecordDTO;

public class MedicalRecordResponse
{
    public string MedicalRecordId { get; set; } = string.Empty;
    public string PatientId { get; set; } = string.Empty;
    public string PatientName { get; set; } = string.Empty;
    public string PatientUsername { get; set; } = string.Empty;
    public string DoctorId { get; set; } = string.Empty;
    public string DoctorName { get; set; } = string.Empty;
    public DateTime ConsultationDate { get; set; }
    public string Diagnosis { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
}

public class MedicalRecordCreateRequest
{
    [Required]
    public string PatientId { get; set; } = string.Empty;

    [Required]
    public DateTime ConsultationDate { get; set; }

    [Required, StringLength(500, MinimumLength = 2)]
    public string Diagnosis { get; set; } = string.Empty;

    [StringLength(2000)]
    public string? Notes { get; set; }
}

public class MedicalRecordUpdateRequest
{
    [Required]
    public string MedicalRecordId { get; set; } = string.Empty;

    [Required]
    public DateTime ConsultationDate { get; set; }

    [Required, StringLength(500, MinimumLength = 2)]
    public string Diagnosis { get; set; } = string.Empty;

    [StringLength(2000)]
    public string? Notes { get; set; }
}
