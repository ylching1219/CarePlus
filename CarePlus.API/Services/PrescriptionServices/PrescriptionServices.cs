using CarePlus.API.Data;
using CarePlus.API.DTO.PrescriptionDTO;
using CarePlus.API.Models.Prescription;
using Microsoft.EntityFrameworkCore;

namespace CarePlus.API.Services.PrescriptionServices;

public class PrescriptionServices(AppDbContext db)
{
    public async Task<List<PrescriptionSummaryResponse>> RetrieveAsync(
        long userId, bool isPharmacist, bool isAdmin, CancellationToken cancellationToken)
    {
        var rows = await AccessiblePrescriptions(userId, isPharmacist, isAdmin)
            .OrderByDescending(row => row.CreatedAt)
            .ThenBy(row => row.PrescriptionId)
            .Select(row => new
            {
                row.PrescriptionId,
                row.PatientId,
                PatientName = row.Patient.User.Name,
                DoctorName = row.Doctor.User.Name,
                row.CreatedAt,
                row.Status
            })
            .ToListAsync(cancellationToken);

        return rows.Select(row => new PrescriptionSummaryResponse(
            row.PrescriptionId,
            row.PatientId,
            row.PatientName,
            row.DoctorName,
            new DateTimeOffset(DateTime.SpecifyKind(row.CreatedAt, DateTimeKind.Unspecified), TimeSpan.FromHours(8)),
            row.Status)).ToList();
    }

    public async Task<List<PrescriptionItemResponse>?> RetrieveItemsAsync(
        long userId, bool isPharmacist, bool isAdmin, string prescriptionId, CancellationToken cancellationToken)
    {
        var prescription = AccessiblePrescriptions(userId, isPharmacist, isAdmin)
            .Where(row => row.PrescriptionId == prescriptionId);
        if (!await prescription.AnyAsync(cancellationToken))
        {
            return null;
        }

        return await db.PrescriptionItems.AsNoTracking()
            .Where(item => prescription.Any(row => row.PrescriptionId == item.PrescriptionId))
            .OrderBy(item => item.Medicine.MedicineName)
            .ThenBy(item => item.PrescriptionItemId)
            .Select(item => new PrescriptionItemResponse(
                item.PrescriptionItemId,
                item.Medicine.MedicineName,
                item.Dosage,
                item.Quantity,
                item.Instructions))
            .ToListAsync(cancellationToken);
    }

    private IQueryable<Prescription> AccessiblePrescriptions(long userId, bool isPharmacist, bool isAdmin)
    {
        var prescriptions = db.Prescriptions.AsNoTracking();
        return isAdmin
            ? prescriptions
            : isPharmacist
            ? prescriptions.Where(_ => db.Pharmacists.Any(pharmacist => pharmacist.UserId == userId))
            : prescriptions.Where(row => row.Doctor.UserId == userId);
    }
}
