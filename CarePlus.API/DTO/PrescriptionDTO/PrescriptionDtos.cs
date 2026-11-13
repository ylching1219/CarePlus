namespace CarePlus.API.DTO.PrescriptionDTO;

public record PrescriptionSummaryResponse(
    string PrescriptionId,
    string PatientId,
    string PatientName,
    string DoctorName,
    DateTimeOffset CreatedAt,
    string Status);

public record PrescriptionItemResponse(
    string PrescriptionItemId,
    string MedicineName,
    string Dosage,
    int Quantity,
    string Instructions);
