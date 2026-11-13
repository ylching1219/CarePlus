using System.Security.Claims;
using CarePlus.API.Data;
using CarePlus.API.Models.PersonalHealthRec;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CarePlus.API.Controllers;

public record PersonalRecordInput(string Type, string? Label, string? Notes, int? Systolic, int? Diastolic, DateTime? RecordedAt);
public record PersonalRecordView(string Id, string Type, string? Label, string? Notes, int? Systolic, int? Diastolic, DateTime RecordedAt, string? PdfFileName);

[ApiController]
[Authorize]
[Route("api/PersonalHealthRecord")]
public class PersonalHealthRecordController(AppDbContext db, IWebHostEnvironment environment) : ControllerBase
{
    private const long MaxPdfBytes = 10 * 1024 * 1024;
    private string PdfDirectory => Path.Combine(environment.ContentRootPath, "private-uploads", "personal-records");

    private async Task<string?> PatientIdAsync()
    {
        if (!long.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId)) return null;
        return await db.Patients.Where(p => p.UserId == userId).Select(p => p.PatientId).FirstOrDefaultAsync();
    }

    private static PersonalRecordView View(PersonalHealthRec row)
    {
        var parts = row.BloodPressure?.Split('/');
        return new(row.HealthRecId, row.RecordType,
            row.RecordType switch { "allergy" => row.Allergies, "condition" => row.HealthConditions, "history" => row.MedicalHistory, _ => null },
            row.HealthNotes,
            parts?.Length == 2 && int.TryParse(parts[0], out var systolic) ? systolic : null,
            parts?.Length == 2 && int.TryParse(parts[1], out var diastolic) ? diastolic : null,
            DateTime.SpecifyKind(row.RecordedAt ?? row.CreatedAt, DateTimeKind.Utc), row.PdfFileName);
    }

    [HttpGet]
    public async Task<IActionResult> List()
    {
        var patientId = await PatientIdAsync();
        if (patientId == null) return Forbid();
        var rows = await db.PersonalHealthRecords.AsNoTracking().Where(r => r.PatientId == patientId)
            .OrderByDescending(r => r.RecordedAt).ToListAsync();
        return Ok(ApiResponse<List<PersonalRecordView>>.Ok(rows.Select(View).ToList()));
    }

    [Authorize(Roles = "Admin,Doctor")]
    [HttpGet("Patient/{patientId}")]
    public async Task<IActionResult> ListForStaff(string patientId)
    {
        var normalizedPatientId = patientId.Trim();
        if (string.IsNullOrWhiteSpace(normalizedPatientId))
        {
            return BadRequest(ApiResponse<List<PersonalRecordView>>.Fail("Patient is required.", 400));
        }

        var patientExists = await db.Patients
            .AsNoTracking()
            .AnyAsync(patient => patient.PatientId == normalizedPatientId);
        if (!patientExists)
        {
            return NotFound(ApiResponse<List<PersonalRecordView>>.Fail("Patient not found.", 404));
        }

        var rows = await db.PersonalHealthRecords
            .AsNoTracking()
            .Where(row => row.PatientId == normalizedPatientId)
            .OrderByDescending(row => row.RecordedAt ?? row.CreatedAt)
            .ToListAsync();

        return Ok(ApiResponse<List<PersonalRecordView>>.Ok(rows.Select(View).ToList()));
    }

    [HttpPost]
    public Task<IActionResult> Save([FromBody] PersonalRecordInput input) => SaveRecord(null, input);

    [HttpPut("{id}")]
    public Task<IActionResult> Update(string id, [FromBody] PersonalRecordInput input) => SaveRecord(id, input);

    private async Task<IActionResult> SaveRecord(string? id, PersonalRecordInput input)
    {
        var patientId = await PatientIdAsync();
        if (patientId == null) return Forbid();
        if (input.Type is not ("bp" or "allergy" or "condition" or "history")) return BadRequest(ApiResponse<object>.Fail("Invalid record type.", 400));
        if (input.Type == "bp")
        {
            if (input.Systolic is < 70 or > 250 || input.Diastolic is < 40 or > 150 || input.Systolic <= input.Diastolic)
                return BadRequest(ApiResponse<object>.Fail("Enter a valid blood pressure reading.", 400));
            if (input.RecordedAt.HasValue && input.RecordedAt.Value.ToUniversalTime() > DateTime.UtcNow)
                return BadRequest(ApiResponse<object>.Fail("Taken at cannot be in the future.", 400));
        }
        else if (string.IsNullOrWhiteSpace(input.Label) || input.Label.Length > 80)
            return BadRequest(ApiResponse<object>.Fail("Enter a name of up to 80 characters.", 400));
        if (input.Notes?.Length > 1000 || input.RecordedAt > DateTime.UtcNow.AddDays(1))
            return BadRequest(ApiResponse<object>.Fail("Invalid notes or date.", 400));

        var row = id == null ? new PersonalHealthRec { HealthRecId = Guid.NewGuid().ToString("N"), PatientId = patientId } :
            await db.PersonalHealthRecords.FirstOrDefaultAsync(r => r.HealthRecId == id && r.PatientId == patientId && r.RecordType != "medical");
        if (row == null) return NotFound(ApiResponse<object>.Fail("Record not found.", 404));
        row.RecordType = input.Type;
        row.BloodPressure = input.Type == "bp" ? $"{input.Systolic}/{input.Diastolic}" : null;
        row.Allergies = input.Type == "allergy" ? input.Label!.Trim() : null;
        row.HealthConditions = input.Type == "condition" ? input.Label!.Trim() : null;
        row.MedicalHistory = input.Type == "history" ? input.Label!.Trim() : null;
        row.HealthNotes = input.Notes?.Trim();
        row.RecordedAt = input.RecordedAt ?? DateTime.UtcNow;
        if (id == null) db.PersonalHealthRecords.Add(row);
        await db.SaveChangesAsync();
        return Ok(ApiResponse<PersonalRecordView>.Ok(View(row)));
    }

    [HttpPost("pdf")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(MaxPdfBytes + 1024 * 1024)]
    public async Task<IActionResult> UploadPdf(IFormFile? file)
    {
        var patientId = await PatientIdAsync();
        if (patientId == null) return Forbid();
        if (file == null || file.Length < 5 || file.Length > MaxPdfBytes || !Path.GetExtension(file.FileName).Equals(".pdf", StringComparison.OrdinalIgnoreCase))
            return BadRequest(ApiResponse<object>.Fail("Choose a PDF smaller than 10 MB.", 400));
        await using var input = file.OpenReadStream();
        var header = new byte[5];
        if (await input.ReadAsync(header) != 5 || !header.SequenceEqual("%PDF-"u8.ToArray()))
            return BadRequest(ApiResponse<object>.Fail("The selected file is not a PDF.", 400));
        input.Position = 0;
        Directory.CreateDirectory(PdfDirectory);
        var storageName = Guid.NewGuid().ToString("N") + ".pdf";
        var path = Path.Combine(PdfDirectory, storageName);
        try
        {
            await using (var output = System.IO.File.Create(path)) await input.CopyToAsync(output);
            var row = new PersonalHealthRec
            {
                HealthRecId = Guid.NewGuid().ToString("N"), PatientId = patientId, RecordType = "medical",
                PdfFileName = Path.GetFileName(file.FileName)[..Math.Min(Path.GetFileName(file.FileName).Length, 255)],
                PdfStorageName = storageName, RecordedAt = DateTime.UtcNow
            };
            db.PersonalHealthRecords.Add(row);
            await db.SaveChangesAsync();
            return Ok(ApiResponse<PersonalRecordView>.Ok(View(row)));
        }
        catch
        {
            System.IO.File.Delete(path);
            throw;
        }
    }

    [HttpGet("{id}/pdf")]
    public async Task<IActionResult> GetPdf(string id)
    {
        var patientId = await PatientIdAsync();
        if (patientId == null) return Forbid();
        var row = await db.PersonalHealthRecords.AsNoTracking().FirstOrDefaultAsync(r => r.HealthRecId == id && r.PatientId == patientId && r.RecordType == "medical");
        if (row?.PdfStorageName == null) return NotFound();
        var path = Path.Combine(PdfDirectory, row.PdfStorageName);
        if (!System.IO.File.Exists(path)) return NotFound();
        return PhysicalFile(path, "application/pdf", enableRangeProcessing: true);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        var patientId = await PatientIdAsync();
        if (patientId == null) return Forbid();
        var row = await db.PersonalHealthRecords.FirstOrDefaultAsync(r => r.HealthRecId == id && r.PatientId == patientId);
        if (row == null) return NotFound(ApiResponse<object>.Fail("Record not found.", 404));
        db.PersonalHealthRecords.Remove(row);
        await db.SaveChangesAsync();
        if (row.PdfStorageName != null) System.IO.File.Delete(Path.Combine(PdfDirectory, row.PdfStorageName));
        return Ok(ApiResponse<object>.Ok(new { deleted = true }));
    }
}
