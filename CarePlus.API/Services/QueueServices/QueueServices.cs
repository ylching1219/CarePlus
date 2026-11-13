using CarePlus.API.Data;
using CarePlus.API.DTO.QueueDTO;
using CarePlus.API.Extension;
using CarePlus.API.Models.Appointment;
using CarePlus.API.Models.Queue;
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

        public QueueServices(AppDbContext db)
        {
            _db = db;
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
                ActiveQueue = active == null ? null : await ToStatusAsync(active),
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
                return ApiResponse<QueueStatusResponse>.Ok(await ToStatusAsync(existing), "You are already checked in.");
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

            var entry = new QueueEntry
            {
                QueueId = $"QUEUE-{appointment.AppointmentId}",
                PatientId = patientId,
                AppointmentId = appointment.AppointmentId,
                QueueType = QueueType(appointment),
                Status = "Waiting",
                CheckInTime = now,
                Appointment = appointment,
                RoomId = await AssignedRoomIdAsync(appointment),
            };
            appointment.CheckInStatus = "CheckedIn";
            _db.QueueEntries.Add(entry);
            await _db.SaveChangesAsync();
            await transaction.CommitAsync();

            return ApiResponse<QueueStatusResponse>.Ok(await ToStatusAsync(entry), "Check-in successful. Your queue number is ready.");
        }

        public async Task<ApiResponse<QueueStatusResponse>> UpdateStatusAsync(long userId, string role, string queueId, string status)
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
            if (entry == null)
            {
                return ApiResponse<QueueStatusResponse>.Fail("Queue entry was not found.", 404);
            }

            if (role.Equals("Doctor", StringComparison.OrdinalIgnoreCase))
            {
                var doctorId = await _db.Doctors.AsNoTracking()
                    .Where(row => row.UserId == userId)
                    .Select(row => row.StaffId)
                    .FirstOrDefaultAsync();
                if (doctorId == null || entry.Appointment?.DoctorId != doctorId)
                {
                    return ApiResponse<QueueStatusResponse>.Fail("You can only update your own appointment queue.", 403);
                }
            }

            entry.Status = normalized;
            if (entry.RoomId == null && entry.Appointment != null)
            {
                entry.RoomId = await AssignedRoomIdAsync(entry.Appointment);
            }
            if (entry.Appointment != null)
            {
                entry.Appointment.CheckInStatus = normalized switch
                {
                    "Completed" => "Completed",
                    "Skipped" => "Missed",
                    "Cancelled" => "Cancelled",
                    _ => normalized,
                };
                if (normalized == "Completed") entry.Appointment.Status = "Completed";
                if (normalized == "Skipped") entry.Appointment.Status = "Missed";
                if (normalized == "Cancelled") entry.Appointment.Status = "Cancelled";
            }

            await _db.SaveChangesAsync();
            return ApiResponse<QueueStatusResponse>.Ok(await ToStatusAsync(entry));
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

        private async Task<QueueStatusResponse> ToStatusAsync(QueueEntry entry)
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
            var roomName = entry.Room?.RoomName;
            if (roomName == null && entry.RoomId != null)
            {
                roomName = await _db.ClinicRooms.AsNoTracking()
                    .Where(row => row.RoomId == entry.RoomId)
                    .Select(row => row.RoomName)
                    .FirstOrDefaultAsync();
            }
            return new QueueStatusResponse
            {
                QueueId = entry.QueueId,
                QueueNumber = $"Q{Math.Max(position, 1):000}",
                AppointmentId = entry.AppointmentId ?? "",
                Status = entry.Status,
                CheckedInAt = ToIso(entry.CheckInTime),
                AppointmentAt = ToIso(StartAt(appointment)),
                DoctorName = DoctorName(appointment),
                VisitType = appointment.VisitType,
                RoomId = entry.RoomId,
                RoomName = roomName,
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

        private async Task<string?> AssignedRoomIdAsync(Appointment appointment)
        {
            var date = DateOnly.FromDateTime(appointment.AppointmentDate);
            var time = TimeOnly.FromTimeSpan(appointment.AppointmentTime);
            return await _db.RoomAssignments.AsNoTracking()
                .Where(row => row.DoctorId == appointment.DoctorId
                    && row.AssignmentDate == date
                    && row.StartTime <= time
                    && row.EndTime > time
                    && row.Room.Status != "Maintenance"
                    && row.Room.Status != "Inactive")
                .OrderBy(row => row.StartTime)
                .Select(row => row.RoomId)
                .FirstOrDefaultAsync();
        }

        private static DateTime StartAt(Appointment row) => row.AppointmentDate.Date + row.AppointmentTime;
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
