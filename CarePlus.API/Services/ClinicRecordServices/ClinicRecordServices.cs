using CarePlus.API.Data;
using CarePlus.API.DTO.ClinicRecordDTO;
using CarePlus.API.Models.Appointment;
using CarePlus.API.Models.MedicalRecord;
using CarePlus.API.Models.PrescriptionItem;
using CarePlus.API.Services.ClinicRecordServices.Interface;
using Microsoft.EntityFrameworkCore;

namespace CarePlus.API.Services.ClinicRecordServices
{
    public class ClinicRecordServices : IClinicRecordServices
    {
        private const decimal SstRate = 0.06m;
        private readonly AppDbContext _db;

        public ClinicRecordServices(AppDbContext db)
        {
            _db = db;
        }

        public async Task<ApiResponse<List<ClinicRecordResponse>>> ListForPatientAsync(long userId, string? locale)
        {
            var patient = await _db.Patients
                .AsNoTracking()
                .Include(row => row.User)
                .FirstOrDefaultAsync(row => row.UserId == userId);
            if (patient == null)
            {
                return ApiResponse<List<ClinicRecordResponse>>.Fail("This account is not a patient account.", 400);
            }

            var records = await _db.MedicalRecords
                .AsNoTracking()
                .Where(row => row.PatientId == patient.PatientId)
                .Include(row => row.Doctor)
                    .ThenInclude(row => row.User)
                .OrderByDescending(row => row.ConsultationDate)
                .ToListAsync();
            if (records.Count == 0)
            {
                return ApiResponse<List<ClinicRecordResponse>>.Ok([]);
            }

            var recordIds = records.Select(row => row.MedicalRecordId).ToList();
            var prescriptionItems = await _db.PrescriptionItems
                .AsNoTracking()
                .Where(row => recordIds.Contains(row.Prescription.MedicalRecordId))
                .Include(row => row.Prescription)
                .Include(row => row.Medicine)
                .ToListAsync();
            var appointments = await _db.Appointments
                .AsNoTracking()
                .Where(row => row.PatientId == patient.PatientId && row.Status == "Completed")
                .ToListAsync();
            var invoices = await _db.Invoices
                .AsNoTracking()
                .Where(row => row.PatientId == patient.PatientId && row.MedicalRecordId != null && recordIds.Contains(row.MedicalRecordId))
                .ToListAsync();
            var visitTypes = await _db.VisitTypes
                .AsNoTracking()
                .ToDictionaryAsync(row => row.VisitTypeId, StringComparer.OrdinalIgnoreCase);

            var isChinese = string.Equals(locale, "zh", StringComparison.OrdinalIgnoreCase);
            var result = records.Select(record => BuildResponse(
                record,
                patient.User.Name ?? "",
                patient.Gender,
                patient.DateOfBirth,
                FindAppointment(record, appointments),
                invoices.FirstOrDefault(row => row.MedicalRecordId == record.MedicalRecordId),
                prescriptionItems.Where(row => row.Prescription.MedicalRecordId == record.MedicalRecordId).ToList(),
                visitTypes,
                isChinese)).ToList();

            return ApiResponse<List<ClinicRecordResponse>>.Ok(result);
        }

        private static Appointment? FindAppointment(MedRecord record, List<Appointment> appointments) => appointments
            .Where(row => row.DoctorId == record.DoctorId && row.AppointmentDate.Date == record.ConsultationDate.Date)
            .OrderBy(row => Math.Abs((row.AppointmentTime - record.ConsultationDate.TimeOfDay).TotalMinutes))
            .FirstOrDefault();

        private static ClinicRecordResponse BuildResponse(
            MedRecord record,
            string patientName,
            char gender,
            DateTime? dateOfBirth,
            Appointment? appointment,
            Models.Invoice.Invoice? invoice,
            List<PrescriptionItem> prescriptionItems,
            Dictionary<string, VisitType> visitTypes,
            bool isChinese)
        {
            var visitTypeId = NormalizeVisitTypeId(appointment?.VisitType, invoice?.VisitType, visitTypes);
            var visitType = visitTypes.GetValueOrDefault(visitTypeId)
                ?? visitTypes.GetValueOrDefault("general")
                ?? new VisitType
                {
                    VisitTypeId = "general",
                    Name = "General consultation",
                    Description = "General clinic consultation.",
                    Price = 50m,
                };
            var visitName = LocalizedVisitName(visitType.VisitTypeId, visitType.Name, isChinese);
            var charges = new List<ClinicChargeLineResponse>
            {
                new()
                {
                    Id = $"{record.MedicalRecordId}-consultation",
                    Category = "consultation",
                    Item = visitName,
                    Detail = visitType.Description,
                    UnitPrice = visitType.Price,
                    Qty = 1,
                },
            };

            foreach (var line in prescriptionItems)
            {
                charges.Add(new ClinicChargeLineResponse
                {
                    Id = line.PrescriptionItemId,
                    Category = "medicine",
                    Item = isChinese && !string.IsNullOrWhiteSpace(line.Medicine.NameZh)
                        ? line.Medicine.NameZh
                        : line.Medicine.MedicineName,
                    Detail = string.Join(" · ", new[] { line.Dosage, line.Instructions }.Where(value => !string.IsNullOrWhiteSpace(value))),
                    UnitPrice = line.Medicine.Price,
                    Qty = Math.Max(line.Quantity, 1),
                });
            }

            var medications = prescriptionItems.Select(line =>
            {
                var name = isChinese && !string.IsNullOrWhiteSpace(line.Medicine.NameZh)
                    ? line.Medicine.NameZh
                    : line.Medicine.MedicineName;
                return string.Join(" · ", new[] { name, line.Dosage, line.Instructions }.Where(value => !string.IsNullOrWhiteSpace(value)));
            }).ToList();

            var visitedAt = new DateTimeOffset(DateTime.SpecifyKind(record.ConsultationDate, DateTimeKind.Unspecified), TimeSpan.FromHours(8));
            return new ClinicRecordResponse
            {
                Id = record.MedicalRecordId,
                PatientName = patientName,
                PatientMeta = PatientMeta(gender, dateOfBirth, record.ConsultationDate, isChinese),
                DoctorId = record.DoctorId,
                DoctorName = record.Doctor.User.Name ?? "",
                Specialty = record.Doctor.Specialization,
                VisitTypeId = visitType.VisitTypeId,
                VisitType = visitName,
                VisitedAt = visitedAt.ToString("yyyy-MM-ddTHH:mm:sszzz"),
                Diagnosis = record.Diagnosis,
                DoctorComment = record.Notes ?? "",
                Medications = medications,
                Ward = "",
                Charges = charges,
                SstRate = SstRate,
            };
        }

        private static string NormalizeVisitTypeId(string? appointmentType, string? invoiceType, Dictionary<string, VisitType> visitTypes)
        {
            var candidate = appointmentType?.Trim();
            if (!string.IsNullOrWhiteSpace(candidate) && visitTypes.ContainsKey(candidate))
            {
                return candidate;
            }

            candidate = invoiceType?.Trim();
            if (!string.IsNullOrWhiteSpace(candidate))
            {
                var match = visitTypes.Values.FirstOrDefault(row =>
                    row.VisitTypeId.Equals(candidate, StringComparison.OrdinalIgnoreCase)
                    || row.Name.Equals(candidate, StringComparison.OrdinalIgnoreCase));
                if (match != null)
                {
                    return match.VisitTypeId;
                }
            }

            return "general";
        }

        private static string LocalizedVisitName(string id, string fallback, bool isChinese)
        {
            if (!isChinese)
            {
                return fallback;
            }

            return id.ToLowerInvariant() switch
            {
                "general" => "普通门诊",
                "mental" => "心理健康咨询",
                "screening" => "身体检查",
                _ => fallback,
            };
        }

        private static string PatientMeta(char gender, DateTime? dateOfBirth, DateTime visitedAt, bool isChinese)
        {
            var genderLabel = char.ToUpperInvariant(gender) == 'M'
                ? (isChinese ? "男" : "Male")
                : (isChinese ? "女" : "Female");
            if (dateOfBirth == null)
            {
                return genderLabel;
            }

            var age = visitedAt.Year - dateOfBirth.Value.Year;
            if (dateOfBirth.Value.Date > visitedAt.Date.AddYears(-age))
            {
                age--;
            }

            return isChinese ? $"{genderLabel}，{Math.Max(age, 0)} 岁" : $"{genderLabel}, {Math.Max(age, 0)} years";
        }
    }
}
