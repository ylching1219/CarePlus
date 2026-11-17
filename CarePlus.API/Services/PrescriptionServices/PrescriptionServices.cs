using CarePlus.API.Data;
using CarePlus.API.DTO.PrescriptionDTO;
using CarePlus.API.Extension;
using CarePlus.API.Models.DispenseRec;
using CarePlus.API.Models.Invoice;
using CarePlus.API.Models.Prescription;
using CarePlus.API.Services.CounterServices.Interface;
using Microsoft.EntityFrameworkCore;

namespace CarePlus.API.Services.PrescriptionServices;

public class PrescriptionServices(AppDbContext db, ICounterServices counters)
{
    private const string Pending = "Pending";
    private const string Preparing = "Preparing";
    private const string Completed = "Completed";
    private const decimal MaxInvoiceAmount = 9999999999999999.99m;

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

    public Task<ApiResponse<PrescriptionSummaryResponse>> MarkPreparingAsync(
        long userId, string prescriptionId, CancellationToken cancellationToken) =>
        ChangeStatusAsync(userId, prescriptionId, Pending, Preparing, cancellationToken);

    public Task<ApiResponse<PrescriptionSummaryResponse>> MarkReadyForPickupAsync(
        long userId, string prescriptionId, CancellationToken cancellationToken) =>
        ChangeStatusAsync(userId, prescriptionId, Preparing, Prescription.ReadyForPickupStatus, cancellationToken);

    public Task<ApiResponse<PrescriptionSummaryResponse>> CompleteDispensingAsync(
        long userId, string prescriptionId, CancellationToken cancellationToken) =>
        ChangeStatusAsync(userId, prescriptionId, Prescription.ReadyForPickupStatus, Completed, cancellationToken);

    private async Task<ApiResponse<PrescriptionSummaryResponse>> ChangeStatusAsync(
        long userId, string prescriptionId, string expectedStatus, string nextStatus, CancellationToken cancellationToken)
    {
        var pharmacistId = await db.Pharmacists
            .AsNoTracking()
            .Where(row => row.UserId == userId)
            .Select(row => row.StaffId)
            .FirstOrDefaultAsync(cancellationToken);
        if (pharmacistId == null && !await db.Admins.AsNoTracking().AnyAsync(row => row.UserId == userId, cancellationToken))
        {
            return ApiResponse<PrescriptionSummaryResponse>.Fail(
                "Only pharmacists and administrators can update pickup status.", 403);
        }

        var id = prescriptionId.Trim();
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

        if (HasStatus(prescription.Status, nextStatus) || (nextStatus == Completed && IsDispensedStatus(prescription.Status)))
        {
            return ApiResponse<PrescriptionSummaryResponse>.Ok(ToSummary(prescription));
        }

        if (!HasStatus(prescription.Status, expectedStatus))
        {
            return ApiResponse<PrescriptionSummaryResponse>.Fail(
                "This action is unavailable for the current prescription status. Refresh the list to see the latest status.", 409);
        }

        try
        {
            prescription.Status = nextStatus;
            await db.SaveChangesAsync(cancellationToken);

            if (nextStatus == Completed)
            {
                var completedAt = DateTimeHelper.MalaysiaNow();
                var medicineTotal = await CalculateMedicineTotalAsync(id, cancellationToken);
                if (medicineTotal == null)
                {
                    return ApiResponse<PrescriptionSummaryResponse>.Fail(
                        "Check the prescription quantities and medicine prices before completing pickup.", 400);
                }

                var billingError = await IssuePickupInvoiceAsync(prescription, medicineTotal.Value, completedAt, cancellationToken);
                if (billingError != null)
                {
                    return ApiResponse<PrescriptionSummaryResponse>.Fail(billingError, 409);
                }

                if (pharmacistId != null && !await db.DispenseRecords.AnyAsync(row => row.PrescriptionId == id, cancellationToken))
                {
                    db.DispenseRecords.Add(new DispenseRec
                    {
                        DispensingId = $"DISP-{Guid.NewGuid():N}",
                        PrescriptionId = id,
                        PharmacistId = pharmacistId,
                        DispenseAt = completedAt,
                        Status = "Dispensed",
                    });
                }

                await db.SaveChangesAsync(cancellationToken);
            }

            await transaction.CommitAsync(cancellationToken);
            return ApiResponse<PrescriptionSummaryResponse>.Ok(ToSummary(prescription));
        }
        catch (DbUpdateConcurrencyException)
        {
            return ApiResponse<PrescriptionSummaryResponse>.Fail(
                "The prescription or invoice counter changed during this update. Refresh the list and try again.", 409);
        }
    }

    private async Task<decimal?> CalculateMedicineTotalAsync(string prescriptionId, CancellationToken cancellationToken)
    {
        var lines = await db.PrescriptionItems.AsNoTracking()
            .Where(item => item.PrescriptionId == prescriptionId)
            .Select(item => new { item.Quantity, item.Medicine.Price })
            .ToListAsync(cancellationToken);

        decimal total = 0;
        foreach (var line in lines)
        {
            if (line.Quantity <= 0 || line.Price <= 0) return null;
            total += line.Price * line.Quantity;
            if (total > MaxInvoiceAmount) return null;
        }

        return total > 0 ? total : null;
    }

    private async Task<string?> IssuePickupInvoiceAsync(
        Prescription prescription, decimal medicineTotal, DateTime issuedAt, CancellationToken cancellationToken)
    {
        var invoices = await db.Invoices
            .Where(row => row.MedicalRecordId == prescription.MedicalRecordId && row.OrderId == null)
            .OrderBy(row => row.IssueAt)
            .ThenBy(row => row.InvoiceId)
            .ToListAsync(cancellationToken);
        var consultationInvoice = invoices.FirstOrDefault(row => !InvoiceVisitLabel.IsPrescription(row.VisitType));
        if (consultationInvoice == null)
        {
            return "The consultation fee is missing for this prescription. Check the visit billing before completing pickup.";
        }

        if (consultationInvoice.PaymentStatus == Invoice.AwaitingPickupStatus)
        {
            var combinedTotal = consultationInvoice.TotalAmount + medicineTotal;
            if (combinedTotal > MaxInvoiceAmount)
            {
                return "The combined consultation and medicine total exceeds the invoice limit.";
            }

            consultationInvoice.TotalAmount = combinedTotal;
            consultationInvoice.IssueAt = issuedAt;
            consultationInvoice.DueDate = issuedAt.Date.AddDays(7);
            consultationInvoice.PaymentStatus = "Pending";
            return null;
        }

        // An issued consultation invoice could already be paid or in an open checkout.
        var billedFor = InvoiceVisitLabel.Read(consultationInvoice.VisitType).BilledFor;
        db.Invoices.Add(new Invoice
        {
            InvoiceId = await counters.GenerateNextIdAsync("InvoiceId"),
            PatientId = consultationInvoice.PatientId,
            MedicalRecordId = prescription.MedicalRecordId,
            RecordRef = prescription.PrescriptionId,
            TotalAmount = medicineTotal,
            IssueAt = issuedAt,
            DueDate = issuedAt.Date.AddDays(7),
            DoctorName = consultationInvoice.DoctorName,
            VisitType = InvoiceVisitLabel.Pack(InvoiceVisitLabel.PrescriptionMedicines, billedFor),
            PaymentStatus = "Pending",
        });
        return null;
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
        var medicalRecordIds = prescriptions.Select(row => row.MedicalRecordId).Distinct().ToList();
        var issuedInvoices = await db.Invoices.AsNoTracking()
            .Where(row => row.OrderId == null && row.DueDate != null && row.MedicalRecordId != null
                && medicalRecordIds.Contains(row.MedicalRecordId))
            .Select(row => new { row.MedicalRecordId, row.RecordRef, row.VisitType, row.IssueAt })
            .ToListAsync(cancellationToken);
        var invoicesByRecord = issuedInvoices.ToLookup(row => row.MedicalRecordId);

        return prescriptions
            .Select(row =>
            {
                if (!dispensedAt.TryGetValue(row.PrescriptionId, out var when))
                {
                    when = invoicesByRecord[row.MedicalRecordId]
                        .Where(invoice => !InvoiceVisitLabel.IsPrescription(invoice.VisitType)
                            || invoice.RecordRef == row.PrescriptionId)
                        .Select(invoice => invoice.IssueAt)
                        .Append(row.CreatedAt)
                        .Max();
                }
                return new DispensedPrescriptionResponse(
                    row.PrescriptionId,
                    new DateTimeOffset(DateTime.SpecifyKind(when, DateTimeKind.Unspecified), TimeSpan.FromHours(8)),
                    row.Status,
                    items.Where(item => item.PrescriptionId == row.PrescriptionId).Select(item => item.Response).ToList());
            })
            .OrderByDescending(row => row.DispensedAt)
            .ToList();
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

    private static bool HasStatus(string? status, string expected) =>
        string.Equals(status?.Trim(), expected, StringComparison.OrdinalIgnoreCase);

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
