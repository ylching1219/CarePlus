using CarePlus.API.Data;
using CarePlus.API.DTO.MedicalRecordDTO;
using CarePlus.API.DTO.QueueDTO;
using CarePlus.API.Extension;
using CarePlus.API.Models.Appointment;
using CarePlus.API.Models.Prescription;
using CarePlus.API.Models.PrescriptionItem;
using CarePlus.API.Models.Queue;
using CarePlus.API.Models.Room;
using CarePlus.API.Models.RoomAssg;
using CarePlus.API.Services.CounterServices.Interface;
using CarePlus.API.Services.MedicalRecordServices.Interface;
using CarePlus.API.Services.QueueServices.Interface;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Globalization;

namespace CarePlus.API.Services.QueueServices
{
    public class QueueServices : IQueueServices
    {
        public const int CheckInMinutesBefore = 10;
        public const int CheckInMinutesAfter = 20;
        public const int EstimatedMinutesPerPatient = 15;

        private static readonly TimeSpan MalaysiaOffset = TimeSpan.FromHours(8);
        private static readonly HashSet<string> ActiveStatuses = new(StringComparer.OrdinalIgnoreCase)
        {
            "Waiting", "Called", "InConsultation"
        };
        private static readonly HashSet<string> AllowedStatuses = new(StringComparer.OrdinalIgnoreCase)
        {
            "Waiting", "Called", "InConsultation", "Completed", "Skipped", "Cancelled"
        };

        private readonly AppDbContext _db;
        private readonly IMedicalRecordServices _medicalRecords;
        private readonly ICounterServices _counters;

        public QueueServices(AppDbContext db, IMedicalRecordServices medicalRecords, ICounterServices counters)
        {
            _db = db;
            _medicalRecords = medicalRecords;
            _counters = counters;
        }

        public async Task<ApiResponse<QueueOverviewResponse>> GetOverviewAsync(long userId, string? appointmentId)
        {
            var patientId = await PatientIdAsync(userId);
            if (patientId == null)
            {
                return ApiResponse<QueueOverviewResponse>.Fail("This account is not a patient account.", 403);
            }

            var active = await QueueQuery()
                .Where(row => row.PatientId == patientId && ActiveStatuses.Contains(row.Status))
                .OrderByDescending(row => row.CheckInTime)
                .FirstOrDefaultAsync();

            var now = DateTimeHelper.MalaysiaNow();
            Appointment? appointment = null;
            if (!string.IsNullOrWhiteSpace(appointmentId))
            {
                appointment = await AppointmentQuery()
                    .FirstOrDefaultAsync(row => row.AppointmentId == appointmentId.Trim() && row.PatientId == patientId);
                if (appointment == null)
                {
                    return ApiResponse<QueueOverviewResponse>.Fail("Appointment was not found.", 404);
                }
            }
            else if (active == null)
            {
                var lower = now.AddMinutes(-CheckInMinutesAfter);
                var upper = now.AddDays(1);
                var candidates = await AppointmentQuery()
                    .Where(row => row.PatientId == patientId && row.Status == "Scheduled")
                    .ToListAsync();
                appointment = candidates
                    .Where(row => StartAt(row) >= lower && StartAt(row) <= upper)
                    .OrderBy(row => StartAt(row))
                    .FirstOrDefault();
            }

            return ApiResponse<QueueOverviewResponse>.Ok(new QueueOverviewResponse
            {
                ActiveQueue = active == null ? null : await ToStatusAsync(active, await ResolveQueueRoomAsync(active)),
                CheckIn = active == null && appointment != null ? ToCheckIn(appointment, now) : null,
            });
        }

        public async Task<ApiResponse<QueueStatusResponse>> CheckInAsync(long userId, string appointmentId)
        {
            var patientId = await PatientIdAsync(userId);
            if (patientId == null)
            {
                return ApiResponse<QueueStatusResponse>.Fail("This account is not a patient account.", 403);
            }

            if (string.IsNullOrWhiteSpace(appointmentId))
            {
                return ApiResponse<QueueStatusResponse>.Fail("Appointment is required.", 400);
            }

            await using var transaction = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            var appointment = await AppointmentQuery(tracking: true)
                .FirstOrDefaultAsync(row => row.AppointmentId == appointmentId.Trim() && row.PatientId == patientId);
            if (appointment == null)
            {
                return ApiResponse<QueueStatusResponse>.Fail("Appointment was not found.", 404);
            }

            var existing = await QueueQuery(tracking: true)
                .FirstOrDefaultAsync(row => row.AppointmentId == appointment.AppointmentId);
            if (existing != null)
            {
                await transaction.CommitAsync();
                return ApiResponse<QueueStatusResponse>.Ok(
                    await ToStatusAsync(existing, await ResolveQueueRoomAsync(existing)),
                    "You are already checked in.");
            }

            if (!appointment.Status.Equals("Scheduled", StringComparison.OrdinalIgnoreCase))
            {
                return ApiResponse<QueueStatusResponse>.Fail("Only scheduled appointments can be checked in.", 400);
            }

            var now = DateTimeHelper.MalaysiaNow();
            var start = StartAt(appointment);
            var opens = start.AddMinutes(-CheckInMinutesBefore);
            var closes = start.AddMinutes(CheckInMinutesAfter);
            if (now < opens)
            {
                return ApiResponse<QueueStatusResponse>.Fail($"Check-in opens at {opens:h:mm tt}.", 400);
            }

            if (now > closes)
            {
                return ApiResponse<QueueStatusResponse>.Fail("The 30-minute check-in window has closed. This appointment will be cancelled automatically.", 400);
            }

            var room = await GetAssignedRoomAsync(appointment);
            var entry = new QueueEntry
            {
                QueueId = $"QUEUE-{appointment.AppointmentId}",
                PatientId = patientId,
                AppointmentId = appointment.AppointmentId,
                QueueType = QueueType(appointment),
                Status = "Waiting",
                CheckInTime = now,
                Appointment = appointment,
                RoomId = room?.RoomId,
            };
            appointment.CheckInStatus = "CheckedIn";
            _db.QueueEntries.Add(entry);
            await _db.SaveChangesAsync();
            await transaction.CommitAsync();

            return ApiResponse<QueueStatusResponse>.Ok(await ToStatusAsync(entry, room), "Check-in successful. Your queue number is ready.");
        }

        public async Task<ApiResponse<List<StaffQueueEntryResponse>>> GetStaffQueueAsync(long userId, string role, DateOnly? date)
        {
            var isAdmin = role.Equals("Admin", StringComparison.OrdinalIgnoreCase);
            if (!isAdmin && !role.Equals("Doctor", StringComparison.OrdinalIgnoreCase))
            {
                return ApiResponse<List<StaffQueueEntryResponse>>.Fail("You do not have permission to view the appointment queue.", 403);
            }

            var day = date?.ToDateTime(TimeOnly.MinValue) ?? DateTimeHelper.MalaysiaNow().Date;
            var entries = await QueueQuery()
                .Where(row => row.Appointment != null && row.Appointment.AppointmentDate == day
                    && (isAdmin || row.Appointment.Doctor.UserId == userId))
                .Include(row => row.Patient).ThenInclude(patient => patient.User)
                .OrderBy(row => row.CheckInTime)
                .ThenBy(row => row.QueueId)
                .ToListAsync();

            if (entries.Count == 0) return ApiResponse<List<StaffQueueEntryResponse>>.Ok([]);

            // Keep the patient-facing number within each doctor, date and shift queue.
            var numbers = entries.GroupBy(row => row.QueueType)
                .SelectMany(group => group.Select((row, index) => new { row.QueueId, Number = QueueNumber(index + 1) }))
                .ToDictionary(row => row.QueueId, row => row.Number);
            var visitTypeIds = entries.Select(row => row.Appointment!.VisitType).Distinct().ToList();
            var visitTypes = await _db.VisitTypes.AsNoTracking()
                .Where(row => visitTypeIds.Contains(row.VisitTypeId))
                .ToDictionaryAsync(row => row.VisitTypeId, row => row.Name);
            var doctorIds = entries.Where(row => ActiveStatuses.Contains(StaffStatus(row, row.Appointment!)))
                .Select(row => row.Appointment!.DoctorId)
                .Distinct()
                .ToArray();
            var assignments = await LoadRoomAssignmentsAsync(day, doctorIds);
            var assignmentsByDoctor = assignments.ToLookup(row => row.DoctorId, StringComparer.OrdinalIgnoreCase);

            return ApiResponse<List<StaffQueueEntryResponse>>.Ok(entries.Select(row =>
            {
                var appointment = row.Appointment!;
                var status = StaffStatus(row, appointment);
                var room = ActiveStatuses.Contains(status)
                    ? FindAssignedRoom(appointment, assignmentsByDoctor[appointment.DoctorId])
                    : row.Room;
                return new StaffQueueEntryResponse
                {
                    QueueId = row.QueueId,
                    QueueNumber = numbers[row.QueueId],
                    AppointmentId = appointment.AppointmentId,
                    PatientName = string.IsNullOrWhiteSpace(appointment.ForName) ? row.Patient.User.Name : appointment.ForName,
                    DoctorName = DoctorName(appointment),
                    VisitType = visitTypes.GetValueOrDefault(appointment.VisitType, appointment.VisitType),
                    RoomName = room?.RoomName,
                    CheckedInAt = ToIso(row.CheckInTime),
                    Status = status,
                };
            }).ToList());
        }

        public async Task<ApiResponse<QueueStatusResponse>> UpdateStatusAsync(long userId, string role, string queueId, string status, string? expectedStatus = null)
        {
            if (!role.Equals("Admin", StringComparison.OrdinalIgnoreCase)
                && !role.Equals("Doctor", StringComparison.OrdinalIgnoreCase))
            {
                return ApiResponse<QueueStatusResponse>.Fail("You do not have permission to update the queue.", 403);
            }

            var normalized = NormalizeStatus(status);
            if (normalized == null)
            {
                return ApiResponse<QueueStatusResponse>.Fail("Choose a valid queue status.", 400);
            }

            var entry = await QueueQuery(tracking: true).FirstOrDefaultAsync(row => row.QueueId == queueId);
            if (entry?.Appointment is not Appointment appointment)
            {
                return ApiResponse<QueueStatusResponse>.Fail("Queue entry was not found.", 404);
            }

            if (role.Equals("Doctor", StringComparison.OrdinalIgnoreCase))
            {
                var doctorId = await _db.Doctors.AsNoTracking()
                    .Where(row => row.UserId == userId)
                    .Select(row => row.StaffId)
                    .FirstOrDefaultAsync();
                if (doctorId == null || appointment.DoctorId != doctorId)
                {
                    return ApiResponse<QueueStatusResponse>.Fail("You can only update your own appointment queue.", 403);
                }
            }

            if (expectedStatus != null && (appointment.Status != "Scheduled"
                || !entry.Status.Equals(expectedStatus, StringComparison.OrdinalIgnoreCase)))
            {
                return ApiResponse<QueueStatusResponse>.Fail("This queue entry has changed. Review its latest status.", 409);
            }

            return await SaveStatusAsync(entry, appointment, normalized);
        }

        public async Task<ApiResponse<QueueStatusResponse>> CompleteConsultationAsync(
            long userId,
            string queueId,
            QueueConsultationRequest request)
        {
            if (request.PrescriptionItems.Any(item => item is null))
            {
                return ApiResponse<QueueStatusResponse>.Fail("Each prescription item must include medicine details.", 400);
            }

            await using var transaction = await _db.Database.BeginTransactionAsync();
            var entry = await QueueQuery(tracking: true).FirstOrDefaultAsync(row =>
                row.QueueId == queueId && row.Appointment != null && row.Appointment.Doctor.UserId == userId);
            if (entry?.Appointment is not Appointment appointment)
            {
                return ApiResponse<QueueStatusResponse>.Fail("Consultation was not found in your queue.", 404);
            }

            if (entry.Status != "InConsultation" || appointment.Status != "Scheduled")
            {
                return ApiResponse<QueueStatusResponse>.Fail("This consultation is no longer in progress. Review the queue for its latest status.", 409);
            }

            var medicineIds = request.PrescriptionItems.Select(item => item.MedicineId.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
            var availableMedicineIds = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (medicineIds.Length > 0)
            {
                availableMedicineIds = await _db.Medicines.AsNoTracking()
                    .Where(medicine => medicineIds.Contains(medicine.MedicineId) && medicine.Status == "Available")
                    .Select(medicine => medicine.MedicineId)
                    .ToDictionaryAsync(id => id, StringComparer.OrdinalIgnoreCase);
                if (medicineIds.Any(id => !availableMedicineIds.ContainsKey(id)))
                {
                    return ApiResponse<QueueStatusResponse>.Fail("A selected medicine is no longer available. Choose an available medicine.", 400);
                }
            }

            var consultationDate = DateTimeHelper.MalaysiaNow();
            var record = await _medicalRecords.CreateAsync(userId, new MedicalRecordCreateRequest
            {
                PatientId = appointment.PatientId,
                ConsultationDate = consultationDate,
                Diagnosis = request.Diagnosis,
                Notes = request.Notes,
            });
            if (!record.IsSuccess || record.Data == null)
            {
                return ApiResponse<QueueStatusResponse>.Fail(record.RespMessage, record.RespCode);
            }

            if (request.PrescriptionItems.Count > 0)
            {
                try
                {
                    var prescription = new Prescription
                    {
                        PrescriptionId = await _counters.GenerateNextIdAsync("PrescriptionId"),
                        MedicalRecordId = record.Data.MedicalRecordId,
                        DoctorId = appointment.DoctorId,
                        PatientId = appointment.PatientId,
                        CreatedAt = consultationDate,
                    };
                    var items = new List<PrescriptionItem>(request.PrescriptionItems.Count);
                    foreach (var item in request.PrescriptionItems)
                    {
                        items.Add(new PrescriptionItem
                        {
                            PrescriptionItemId = await _counters.GenerateNextIdAsync("PrescriptionItemId"),
                            Prescription = prescription,
                            MedicineId = availableMedicineIds[item.MedicineId.Trim()],
                            Dosage = item.Dosage.Trim(),
                            Quantity = item.Quantity,
                            Instructions = item.Instructions.Trim(),
                        });
                    }

                    _db.Prescriptions.Add(prescription);
                    _db.PrescriptionItems.AddRange(items);
                }
                catch (DbUpdateConcurrencyException)
                {
                    return ApiResponse<QueueStatusResponse>.Fail("Another prescription was saved at the same time. Try ending the consultation again.", 409);
                }
            }

            var result = await SaveStatusAsync(entry, appointment, "Completed");
            if (result.IsSuccess)
            {
                await transaction.CommitAsync();
            }
            return result;
        }

        private async Task<ApiResponse<QueueStatusResponse>> SaveStatusAsync(
            QueueEntry entry,
            Appointment appointment,
            string normalized)
        {
            var room = entry.Room;
            if (ActiveStatuses.Contains(normalized))
            {
                room = await GetAssignedRoomAsync(appointment);
            }
            else if (ActiveStatuses.Contains(StaffStatus(entry, appointment)) || room == null)
            {
                room = await GetAssignedRoomAsync(appointment) ?? room;
            }

            entry.RoomId = room?.RoomId;
            entry.Status = normalized;

            appointment.CheckInStatus = normalized switch
            {
                "Completed" => "Completed",
                "Skipped" => "Missed",
                "Cancelled" => "Cancelled",
                _ => normalized,
            };
            if (normalized == "Completed") appointment.Status = "Completed";
            if (normalized == "Skipped") appointment.Status = "Missed";
            if (normalized == "Cancelled") appointment.Status = "Cancelled";

            try
            {
                await _db.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                return ApiResponse<QueueStatusResponse>.Fail("This queue entry has changed. Review its latest status.", 409);
            }
            return ApiResponse<QueueStatusResponse>.Ok(await ToStatusAsync(entry, room));
        }

        private IQueryable<QueueEntry> QueueQuery(bool tracking = false)
        {
            var query = _db.QueueEntries
                .Include(row => row.Appointment)!
                    .ThenInclude(row => row!.Doctor)
                    .ThenInclude(row => row.User)
                .Include(row => row.Room)
                .AsQueryable();
            return tracking ? query : query.AsNoTracking();
        }

        private IQueryable<Appointment> AppointmentQuery(bool tracking = false)
        {
            var query = _db.Appointments
                .Include(row => row.Doctor)
                    .ThenInclude(row => row.User)
                .AsQueryable();
            return tracking ? query : query.AsNoTracking();
        }

        private async Task<QueueStatusResponse> ToStatusAsync(QueueEntry entry, ClinicRoom? room)
        {
            var group = await _db.QueueEntries.AsNoTracking()
                .Where(row => row.QueueType == entry.QueueType)
                .OrderBy(row => row.CheckInTime)
                .ThenBy(row => row.QueueId)
                .ToListAsync();
            var position = group.FindIndex(row => row.QueueId == entry.QueueId) + 1;
            var peopleAhead = group.Count(row =>
                row.QueueId != entry.QueueId
                && row.CheckInTime <= entry.CheckInTime
                && ActiveStatuses.Contains(row.Status));
            if (!entry.Status.Equals("Waiting", StringComparison.OrdinalIgnoreCase)) peopleAhead = 0;

            var appointment = entry.Appointment!;
            return new QueueStatusResponse
            {
                QueueId = entry.QueueId,
                QueueNumber = QueueNumber(Math.Max(position, 1)),
                AppointmentId = entry.AppointmentId ?? "",
                Status = entry.Status,
                CheckedInAt = ToIso(entry.CheckInTime),
                AppointmentAt = ToIso(StartAt(appointment)),
                DoctorName = DoctorName(appointment),
                VisitType = appointment.VisitType,
                RoomId = room?.RoomId,
                RoomName = room?.RoomName,
                PeopleAhead = peopleAhead,
                EstimatedWaitMinutes = peopleAhead * EstimatedMinutesPerPatient,
            };
        }

        private static QueueCheckInResponse ToCheckIn(Appointment appointment, DateTime now)
        {
            var start = StartAt(appointment);
            var opens = start.AddMinutes(-CheckInMinutesBefore);
            var closes = start.AddMinutes(CheckInMinutesAfter);
            var canCheckIn = now >= opens && now <= closes && appointment.CheckInStatus.Equals("Pending", StringComparison.OrdinalIgnoreCase);
            var message = now < opens
                ? $"Check-in opens at {opens:h:mm tt}, ten minutes before your appointment."
                : now > closes
                    ? "The 30-minute check-in window has closed. This appointment will be cancelled automatically."
                    : canCheckIn
                        ? "You can check in now to receive your queue number."
                        : "This appointment has already been checked in.";
            return new QueueCheckInResponse
            {
                AppointmentId = appointment.AppointmentId,
                AppointmentAt = ToIso(start),
                DoctorName = DoctorName(appointment),
                VisitType = appointment.VisitType,
                CheckInOpensAt = ToIso(opens),
                CheckInClosesAt = ToIso(closes),
                CanCheckIn = canCheckIn,
                Message = message,
            };
        }

        private async Task<string?> PatientIdAsync(long userId) => await _db.Patients.AsNoTracking()
            .Where(row => row.UserId == userId)
            .Select(row => row.PatientId)
            .FirstOrDefaultAsync();

        private Task<ClinicRoom?> ResolveQueueRoomAsync(QueueEntry entry) =>
            entry.Appointment is Appointment appointment && ActiveStatuses.Contains(StaffStatus(entry, appointment))
                ? GetAssignedRoomAsync(appointment)
                : Task.FromResult(entry.Room);

        private async Task<ClinicRoom?> GetAssignedRoomAsync(Appointment appointment)
        {
            var assignments = await LoadRoomAssignmentsAsync(appointment.AppointmentDate, [appointment.DoctorId]);
            return FindAssignedRoom(appointment, assignments);
        }

        private async Task<List<RoomAssignment>> LoadRoomAssignmentsAsync(DateTime day, string[] doctorIds)
        {
            if (doctorIds.Length == 0)
            {
                return [];
            }

            var date = DateOnly.FromDateTime(day);
            return await _db.RoomAssignments.AsNoTracking()
                .Where(row => doctorIds.Contains(row.DoctorId)
                    && row.AssignmentDate == date
                    && row.Schedule != null
                    && row.Schedule.Status == "Active"
                    && row.Room.Status != "Maintenance"
                    && row.Room.Status != "Inactive")
                .Include(row => row.Room)
                .OrderBy(row => row.StartTime)
                .ToListAsync();
        }

        private static ClinicRoom? FindAssignedRoom(Appointment appointment, IEnumerable<RoomAssignment> assignments)
        {
            var time = TimeOnly.FromTimeSpan(appointment.AppointmentTime);
            return assignments.FirstOrDefault(row => time.IsBetween(row.StartTime, row.EndTime))?.Room;
        }

        private static DateTime StartAt(Appointment row) => row.AppointmentDate.Date + row.AppointmentTime;
        private static string QueueNumber(int position) => $"Q{position:000}";
        private static string StaffStatus(QueueEntry entry, Appointment appointment) => appointment.Status switch
        {
            "Completed" => "Completed",
            "Missed" => "Skipped",
            "Cancelled" => "Cancelled",
            _ => entry.Status,
        };
        private static string QueueType(Appointment row) => $"appointment:{row.DoctorId}:{row.AppointmentDate:yyyyMMdd}:{(row.AppointmentTime < TimeSpan.FromHours(18) ? "morning" : "night")}";
        private static string DoctorName(Appointment row) => string.IsNullOrWhiteSpace(row.Doctor?.User?.Name) ? "CarePlus doctor" : $"Dr {row.Doctor.User.Name.Trim().Replace("Dr ", "", StringComparison.OrdinalIgnoreCase)}";
        private static string ToIso(DateTime value) => new DateTimeOffset(DateTime.SpecifyKind(value, DateTimeKind.Unspecified), MalaysiaOffset).ToString("yyyy-MM-ddTHH:mm:sszzz", CultureInfo.InvariantCulture);

        private static string? NormalizeStatus(string value)
        {
            var match = AllowedStatuses.FirstOrDefault(item => item.Equals((value ?? "").Trim(), StringComparison.OrdinalIgnoreCase));
            return match;
        }
    }
}
