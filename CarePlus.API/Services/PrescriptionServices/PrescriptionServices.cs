using CarePlus.API.Data;
using CarePlus.API.DTO.PrescriptionDTO;
using CarePlus.API.Extension;
using CarePlus.API.Models.DispenseRec;
using CarePlus.API.Models.Invoice;
using CarePlus.API.Models.Prescription;
using CarePlus.API.Models.PrescriptionItem;
using CarePlus.API.Services.CounterServices.Interface;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace CarePlus.API.Services.PrescriptionServices;

public class PrescriptionServices(AppDbContext db, ICounterServices counters)
{
    private const decimal SstRate = 0.06m;
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

    public async Task<ApiResponse<object>> MarkReadyForPickupAsync(
        long userId, string prescriptionId, CancellationToken cancellationToken)
    {
        var pharmacist = await db.Pharmacists.AsNoTracking()
            .FirstOrDefaultAsync(row => row.UserId == userId, cancellationToken);
        if (pharmacist == null)
        {
            return ApiResponse<object>.Fail("Only a pharmacist can prepare prescriptions.", 403);
        }

        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        try
        {
            var prescription = await db.Prescriptions
                .Include(row => row.Patient)
                    .ThenInclude(row => row.User)
                .Include(row => row.Doctor)
                    .ThenInclude(row => row.User)
                .Include(row => row.MedicalRecord)
                .FirstOrDefaultAsync(row => row.PrescriptionId == prescriptionId, cancellationToken);
            if (prescription == null)
            {
                return ApiResponse<object>.Fail("Prescription not found.", 404);
            }

            if (prescription.Status == "Ready for pickup")
            {
                return ApiResponse<object>.Ok(new { prescriptionId, status = prescription.Status }, "Prescription is already ready for pickup.");
            }

            if (prescription.Status != "Pending")
            {
                return ApiResponse<object>.Fail($"This prescription is already {prescription.Status.ToLowerInvariant()}.", 409);
            }

            var items = await db.PrescriptionItems
                .Include(row => row.Medicine)
                .Where(row => row.PrescriptionId == prescriptionId)
                .ToListAsync(cancellationToken);
            if (items.Count == 0)
            {
                return ApiResponse<object>.Fail("This prescription has no medicine items.", 400);
            }

            var requirements = items
                .GroupBy(row => row.MedicineId)
                .Select(group => new
                {
                    MedicineId = group.Key,
                    MedicineName = group.Select(row => row.Medicine.MedicineName).FirstOrDefault() ?? group.Key,
                    Quantity = group.Sum(row => (long)row.Quantity),
                })
                .ToList();
            var medicineIds = requirements.Select(row => row.MedicineId).ToArray();
            var today = DateTimeHelper.MalaysiaNow().Date;
            var batches = await db.MedicineBatches
                .Where(row => medicineIds.Contains(row.MedicineId)
                    && row.StockQuantity > 0
                    && row.ExpiryDate >= today)
                .OrderBy(row => row.ExpiryDate)
                .ThenBy(row => row.BatchId)
                .ToListAsync(cancellationToken);

            foreach (var requirement in requirements)
            {
                var available = batches
                    .Where(row => row.MedicineId == requirement.MedicineId)
                    .Sum(row => (long)row.StockQuantity);
                if (available < requirement.Quantity)
                {
                    return ApiResponse<object>.Fail($"Insufficient stock for {requirement.MedicineName}. Required: {requirement.Quantity}; available: {available}.", 409);
                }
            }

            foreach (var requirement in requirements)
            {
                var remaining = requirement.Quantity;
                foreach (var batch in batches.Where(row => row.MedicineId == requirement.MedicineId))
                {
                    if (remaining <= 0) break;
                    var issued = Math.Min((long)batch.StockQuantity, remaining);
                    batch.StockQuantity -= (int)issued;
                    remaining -= issued;
                }
            }

            var now = DateTimeHelper.MalaysiaNow();
            prescription.Status = "Ready for pickup";
            db.DispenseRecords.Add(new DispenseRec
            {
                DispensingId = CreateDispensingId(),
                PrescriptionId = prescription.PrescriptionId,
                PharmacistId = pharmacist.StaffId,
                DispenseAt = now,
                Status = "Ready for pickup",
            });
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return ApiResponse<object>.Ok(new { prescriptionId, status = prescription.Status }, "Prescription is ready for pickup.");
        }
        catch (DbUpdateConcurrencyException)
        {
            await transaction.RollbackAsync(cancellationToken);
            return ApiResponse<object>.Fail("The prescription or stock changed while preparing it. Refresh and try again.", 409);
        }
        catch (Exception)
        {
            await transaction.RollbackAsync(cancellationToken);
            return ApiResponse<object>.Fail("Unable to prepare the prescription right now.", 500);
        }
    }

    public async Task<ApiResponse<object>> CompletePrescriptionAsync(
        long userId, string prescriptionId, CancellationToken cancellationToken)
    {
        var pharmacist = await db.Pharmacists.AsNoTracking()
            .FirstOrDefaultAsync(row => row.UserId == userId, cancellationToken);
        if (pharmacist == null)
        {
            return ApiResponse<object>.Fail("Only a pharmacist can confirm pickup.", 403);
        }

        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        try
        {
            var prescription = await db.Prescriptions
                .Include(row => row.Doctor)
                    .ThenInclude(row => row.User)
                .Include(row => row.MedicalRecord)
                .FirstOrDefaultAsync(row => row.PrescriptionId == prescriptionId, cancellationToken);
            if (prescription == null)
            {
                return ApiResponse<object>.Fail("Prescription not found.", 404);
            }

            if (prescription.Status == "Completed")
            {
                return ApiResponse<object>.Ok(new { prescriptionId, status = prescription.Status }, "Prescription was already completed.");
            }

            if (prescription.Status != "Ready for pickup")
            {
                return ApiResponse<object>.Fail("The prescription must be ready for pickup first.", 409);
            }

            var items = await db.PrescriptionItems
                .Include(row => row.Medicine)
                .Where(row => row.PrescriptionId == prescriptionId)
                .ToListAsync(cancellationToken);
            if (items.Count == 0)
            {
                return ApiResponse<object>.Fail("This prescription has no medicine items.", 400);
            }

            var record = await db.DispenseRecords
                .Where(row => row.PrescriptionId == prescriptionId && row.Status == "Ready for pickup")
                .OrderByDescending(row => row.DispenseAt)
                .FirstOrDefaultAsync(cancellationToken);
            if (record == null)
            {
                return ApiResponse<object>.Fail("The pickup record could not be found. Refresh and try again.", 409);
            }

            prescription.Status = "Completed";
            record.Status = "Completed";
            var now = DateTimeHelper.MalaysiaNow();
            record.DispenseAt = now;
            var invoice = await EnsurePrescriptionInvoiceAsync(prescription, items, now, cancellationToken);
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return ApiResponse<object>.Ok(new { prescriptionId, status = prescription.Status, invoiceId = invoice.InvoiceId }, "Prescription completed and payment bill created.");
        }
        catch (DbUpdateConcurrencyException)
        {
            await transaction.RollbackAsync(cancellationToken);
            return ApiResponse<object>.Fail("The prescription changed while confirming pickup. Refresh and try again.", 409);
        }
    }

    private async Task<Invoice> EnsurePrescriptionInvoiceAsync(
        Prescription prescription, List<PrescriptionItem> items, DateTime issueAt, CancellationToken cancellationToken)
    {
        var existing = await db.Invoices
            .FirstOrDefaultAsync(row => row.MedicalRecordId == prescription.MedicalRecordId, cancellationToken);
        if (existing != null) return existing;

        var consultationDate = prescription.MedicalRecord.ConsultationDate;
        var visitTypeId = await db.Appointments.AsNoTracking()
            .Where(row => row.PatientId == prescription.PatientId
                && row.DoctorId == prescription.DoctorId
                && row.AppointmentDate >= consultationDate.Date
                && row.AppointmentDate < consultationDate.Date.AddDays(1))
            .OrderBy(row => row.AppointmentTime)
            .Select(row => row.VisitType)
            .FirstOrDefaultAsync(cancellationToken);
        var visitType = await db.VisitTypes.AsNoTracking()
            .FirstOrDefaultAsync(row => row.VisitTypeId == visitTypeId, cancellationToken);
        var consultationFee = visitType?.Price ?? 50m;
        var medicineTotal = items.Sum(item => item.Medicine.Price * item.Quantity);
        var subtotal = consultationFee + medicineTotal;
        var total = decimal.Round(subtotal * (1 + SstRate), 2, MidpointRounding.AwayFromZero);
        var invoice = new Invoice
        {
            InvoiceId = await counters.GenerateNextIdAsync("InvoiceId"),
            PatientId = prescription.PatientId,
            MedicalRecordId = prescription.MedicalRecordId,
            TotalAmount = total,
            IssueAt = issueAt,
            DueDate = issueAt.Date,
            DoctorName = prescription.Doctor.User.Name,
            VisitType = visitType?.Name ?? "General consultation",
            RecordRef = prescription.MedicalRecordId,
            PaymentStatus = "Pending",
        };
        db.Invoices.Add(invoice);
        return invoice;
    }

    private static string CreateDispensingId() => $"DSP-{DateTimeHelper.MalaysiaNow():yyyyMMddHHmmssfff}-{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}";

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
