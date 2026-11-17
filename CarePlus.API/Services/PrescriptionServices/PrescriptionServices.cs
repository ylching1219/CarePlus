using CarePlus.API.Data;
using CarePlus.API.DTO.PrescriptionDTO;
using CarePlus.API.Extension;
using CarePlus.API.Models.DispenseRec;
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

    public async Task<ApiResponse<PrescriptionSummaryResponse>> CompleteDispensingAsync(
        long userId, string prescriptionId, CancellationToken cancellationToken)
    {
        var pharmacistId = await db.Pharmacists
            .AsNoTracking()
            .Where(row => row.UserId == userId)
            .Select(row => row.StaffId)
            .FirstOrDefaultAsync(cancellationToken);
        var isAdmin = await db.Admins.AsNoTracking().AnyAsync(row => row.UserId == userId, cancellationToken);
        if (string.IsNullOrWhiteSpace(pharmacistId) && !isAdmin)
        {
            return ApiResponse<PrescriptionSummaryResponse>.Fail(
                "Only an active pharmacist can complete prescription dispensing.", 403);
        }

        var id = (prescriptionId ?? string.Empty).Trim();
        if (id.Length == 0)
        {
            return ApiResponse<PrescriptionSummaryResponse>.Fail("Prescription is required.", 400);
        }

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var prescription = await db.Prescriptions
            .Include(row => row.Patient)
                .ThenInclude(row => row.User)
            .Include(row => row.Doctor)
                .ThenInclude(row => row.User)
            .FirstOrDefaultAsync(row => row.PrescriptionId == id, cancellationToken);
        if (prescription == null)
        {
            return ApiResponse<PrescriptionSummaryResponse>.Fail("Prescription not found.", 404);
        }

        if (IsClosedStatus(prescription.Status))
        {
            await transaction.CommitAsync(cancellationToken);
            return ApiResponse<PrescriptionSummaryResponse>.Ok(
                ToSummary(prescription), "Prescription is already completed.");
        }

        var existingRecord = await db.DispenseRecords
            .FirstOrDefaultAsync(row => row.PrescriptionId == id, cancellationToken);
        if (existingRecord == null && !string.IsNullOrWhiteSpace(pharmacistId))
        {
            db.DispenseRecords.Add(new DispenseRec
            {
                DispensingId = $"DISP-{Guid.NewGuid():N}",
                PrescriptionId = id,
                PharmacistId = pharmacistId,
                DispenseAt = DateTimeHelper.MalaysiaNow(),
                Status = "Dispensed",
            });
        }

        prescription.Status = "Completed";
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return ApiResponse<PrescriptionSummaryResponse>.Ok(
            ToSummary(prescription), "Prescription dispensing completed.");
    }

    public async Task<ApiResponse<PrescriptionSummaryResponse>> MarkPreparingAsync(
        long userId, string prescriptionId, CancellationToken cancellationToken)
    {
        if (!await CanPrepareAsync(userId, cancellationToken))
        {
            return ApiResponse<PrescriptionSummaryResponse>.Fail(
                "Only pharmacy staff can prepare a prescription.", 403);
        }

        var id = (prescriptionId ?? string.Empty).Trim();
        if (id.Length == 0)
        {
            return ApiResponse<PrescriptionSummaryResponse>.Fail("Prescription is required.", 400);
        }

        var prescription = await db.Prescriptions
            .Include(row => row.Patient).ThenInclude(row => row.User)
            .Include(row => row.Doctor).ThenInclude(row => row.User)
            .FirstOrDefaultAsync(row => row.PrescriptionId == id, cancellationToken);
        if (prescription == null)
        {
            return ApiResponse<PrescriptionSummaryResponse>.Fail("Prescription not found.", 404);
        }

        if (IsClosedStatus(prescription.Status))
        {
            return ApiResponse<PrescriptionSummaryResponse>.Fail("This prescription is already completed.", 400);
        }

        if (!string.Equals(prescription.Status?.Trim(), "Preparing", StringComparison.OrdinalIgnoreCase))
        {
            prescription.Status = "Preparing";
            await db.SaveChangesAsync(cancellationToken);
        }

        return ApiResponse<PrescriptionSummaryResponse>.Ok(ToSummary(prescription), "Prescription is being prepared.");
    }

    public async Task<List<DispensedPrescriptionResponse>> ListDispensedForPatientAsync(
        long userId, CancellationToken cancellationToken)
    {
        var patientId = await db.Patients.AsNoTracking()
            .Where(row => row.UserId == userId)
            .Select(row => row.PatientId)
            .FirstOrDefaultAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(patientId)) return [];

        var prescriptions = await db.Prescriptions.AsNoTracking()
            .Where(row => row.PatientId == patientId)
            .ToListAsync(cancellationToken);
        prescriptions = prescriptions.Where(row => IsDispensedStatus(row.Status)).ToList();
        if (prescriptions.Count == 0) return [];

        var ids = prescriptions.Select(row => row.PrescriptionId).ToList();
        var items = await db.PrescriptionItems.AsNoTracking()
            .Where(item => ids.Contains(item.PrescriptionId))
            .OrderBy(item => item.Medicine.MedicineName)
            .Select(item => new
            {
                item.PrescriptionId,
                Response = new PrescriptionItemResponse(
                    item.PrescriptionItemId,
                    item.Medicine.MedicineName,
                    item.Dosage,
                    item.Quantity,
                    item.Instructions),
            })
            .ToListAsync(cancellationToken);
        var dispensedAt = await db.DispenseRecords.AsNoTracking()
            .Where(row => ids.Contains(row.PrescriptionId))
            .GroupBy(row => row.PrescriptionId)
            .Select(group => new { PrescriptionId = group.Key, At = group.Max(row => row.DispenseAt) })
            .ToDictionaryAsync(row => row.PrescriptionId, row => row.At, cancellationToken);

        return prescriptions
            .Select(row =>
            {
                var when = dispensedAt.GetValueOrDefault(row.PrescriptionId, row.CreatedAt);
                return new DispensedPrescriptionResponse(
                    row.PrescriptionId,
                    new DateTimeOffset(DateTime.SpecifyKind(when, DateTimeKind.Unspecified), TimeSpan.FromHours(8)),
                    row.Status,
                    items.Where(item => item.PrescriptionId == row.PrescriptionId).Select(item => item.Response).ToList());
            })
            .OrderByDescending(row => row.DispensedAt)
            .ToList();
    }

    private async Task<bool> CanPrepareAsync(long userId, CancellationToken cancellationToken) =>
        await db.Pharmacists.AsNoTracking().AnyAsync(row => row.UserId == userId, cancellationToken)
        || await db.Admins.AsNoTracking().AnyAsync(row => row.UserId == userId, cancellationToken);

    private IQueryable<Prescription> AccessiblePrescriptions(long userId, bool isPharmacist, bool isAdmin)
    {
        var prescriptions = db.Prescriptions.AsNoTracking();
        return isAdmin
            ? prescriptions
            : isPharmacist
            ? prescriptions.Where(_ => db.Pharmacists.Any(pharmacist => pharmacist.UserId == userId))
            : prescriptions.Where(row => row.Doctor.UserId == userId);
    }

    private static bool IsClosedStatus(string? status) =>
        status?.Trim().ToLowerInvariant() is "completed" or "dispensed" or "picked_up" or "cancelled";

    private static bool IsDispensedStatus(string? status) =>
        status?.Trim().ToLowerInvariant() is "completed" or "dispensed" or "picked_up";

    private static PrescriptionSummaryResponse ToSummary(Prescription row) => new(
        row.PrescriptionId,
        row.PatientId,
        row.Patient.User.Name ?? row.PatientId,
        row.Doctor.User.Name ?? row.Doctor.StaffId,
        new DateTimeOffset(DateTime.SpecifyKind(row.CreatedAt, DateTimeKind.Unspecified), TimeSpan.FromHours(8)),
        row.Status);
}
