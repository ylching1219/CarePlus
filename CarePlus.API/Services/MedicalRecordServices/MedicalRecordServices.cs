using CarePlus.API.Data;
using CarePlus.API.DTO.MedicalRecordDTO;
using CarePlus.API.Extension;
using CarePlus.API.Models.MedicalRecord;
using CarePlus.API.Services.MedicalRecordServices.Interface;
using Microsoft.EntityFrameworkCore;

namespace CarePlus.API.Services.MedicalRecordServices;

public class MedicalRecordServices : IMedicalRecordServices
{
    private readonly AppDbContext _db;

    public MedicalRecordServices(AppDbContext db)
    {
        _db = db;
    }

    public async Task<ApiResponse<List<MedicalRecordResponse>>> RetrieveForDoctorAsync(long userId)
    {
        var doctor = await FindDoctorAsync(userId);
        if (doctor == null)
        {
            return ApiResponse<List<MedicalRecordResponse>>.Fail(
                "This account is not a doctor account.", 403);
        }

        var records = await _db.MedicalRecords
            .AsNoTracking()
            .Where(record => record.DoctorId == doctor.StaffId)
            .Include(record => record.Patient)
                .ThenInclude(patient => patient.User)
            .Include(record => record.Doctor)
                .ThenInclude(recordDoctor => recordDoctor.User)
            .OrderByDescending(record => record.ConsultationDate)
            .ToListAsync();

        return ApiResponse<List<MedicalRecordResponse>>.Ok(
            records.Select(ToResponse).ToList());
    }

    public async Task<ApiResponse<MedicalRecordResponse>> CreateAsync(
        long userId,
        MedicalRecordCreateRequest request)
    {
        var doctor = await FindDoctorAsync(userId);
        if (doctor == null)
        {
            return ApiResponse<MedicalRecordResponse>.Fail(
                "This account is not a doctor account.", 403);
        }

        var validationError = ValidateRecord(request.PatientId, request.ConsultationDate, request.Diagnosis);
        if (validationError != null)
        {
            return ApiResponse<MedicalRecordResponse>.Fail(validationError, 400);
        }

        var patient = await _db.Patients
            .Include(row => row.User)
            .FirstOrDefaultAsync(row => row.PatientId == request.PatientId.Trim());
        if (patient == null)
        {
            return ApiResponse<MedicalRecordResponse>.Fail("Patient not found.", 404);
        }

        var record = new MedRecord
        {
            MedicalRecordId = CreateMedicalRecordId(),
            PatientId = patient.PatientId,
            DoctorId = doctor.StaffId,
            ConsultationDate = request.ConsultationDate,
            Diagnosis = request.Diagnosis.Trim(),
            Notes = NormalizeNotes(request.Notes),
        };

        await _db.MedicalRecords.AddAsync(record);
        await _db.SaveChangesAsync();

        record.Patient = patient;
        record.Doctor = doctor;
        return ApiResponse<MedicalRecordResponse>.Ok(ToResponse(record));
    }

    public async Task<ApiResponse<MedicalRecordResponse>> UpdateAsync(
        long userId,
        MedicalRecordUpdateRequest request)
    {
        var doctor = await FindDoctorAsync(userId);
        if (doctor == null)
        {
            return ApiResponse<MedicalRecordResponse>.Fail(
                "This account is not a doctor account.", 403);
        }

        var validationError = ValidateRecord(string.Empty, request.ConsultationDate, request.Diagnosis, false);
        if (validationError != null)
        {
            return ApiResponse<MedicalRecordResponse>.Fail(validationError, 400);
        }

        var record = await _db.MedicalRecords
            .Include(row => row.Patient)
                .ThenInclude(patient => patient.User)
            .Include(row => row.Doctor)
                .ThenInclude(recordDoctor => recordDoctor.User)
            .FirstOrDefaultAsync(row => row.MedicalRecordId == request.MedicalRecordId.Trim()
                && row.DoctorId == doctor.StaffId);
        if (record == null)
        {
            return ApiResponse<MedicalRecordResponse>.Fail(
                "Medical record not found or does not belong to you.", 404);
        }

        record.ConsultationDate = request.ConsultationDate;
        record.Diagnosis = request.Diagnosis.Trim();
        record.Notes = NormalizeNotes(request.Notes);
        await _db.SaveChangesAsync();

        return ApiResponse<MedicalRecordResponse>.Ok(ToResponse(record));
    }

    private Task<Models.User.Staff.Doctor.Doctor?> FindDoctorAsync(long userId) =>
        _db.Doctors
            .Include(doctor => doctor.User)
            .FirstOrDefaultAsync(doctor => doctor.UserId == userId);

    private static MedicalRecordResponse ToResponse(MedRecord record) => new()
    {
        MedicalRecordId = record.MedicalRecordId,
        PatientId = record.PatientId,
        PatientName = record.Patient?.User?.Name ?? record.PatientId,
        PatientUsername = record.Patient?.User?.Username ?? string.Empty,
        DoctorId = record.DoctorId,
        DoctorName = record.Doctor?.User?.Name ?? record.DoctorId,
        ConsultationDate = record.ConsultationDate,
        Diagnosis = record.Diagnosis,
        Notes = record.Notes ?? string.Empty,
    };

    private static string? ValidateRecord(
        string patientId,
        DateTime consultationDate,
        string diagnosis,
        bool validatePatient = true)
    {
        if (validatePatient && string.IsNullOrWhiteSpace(patientId))
        {
            return "Patient is required.";
        }

        if (consultationDate == default)
        {
            return "Consultation date is required.";
        }

        if (consultationDate > DateTimeHelper.MalaysiaNow().AddMinutes(5))
        {
            return "Consultation date cannot be in the future.";
        }

        if (string.IsNullOrWhiteSpace(diagnosis) || diagnosis.Trim().Length < 2)
        {
            return "Diagnosis is required.";
        }

        return null;
    }

    private static string? NormalizeNotes(string? notes) =>
        string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();

    private static string CreateMedicalRecordId() =>
        $"MR-{DateTimeHelper.MalaysiaNow():yyyyMMddHHmmss}-{Guid.NewGuid():N}"[..23].ToUpperInvariant();
}
