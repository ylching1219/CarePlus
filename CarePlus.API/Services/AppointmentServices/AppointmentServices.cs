using CarePlus.API.Data;
using CarePlus.API.DTO.AppointmentDTO;
using CarePlus.API.Extension;
using CarePlus.API.Models.Appointment;
using CarePlus.API.Services.AppointmentServices.Interface;
using CarePlus.API.Services.CounterServices.Interface;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace CarePlus.API.Services.AppointmentServices
{
    public class AppointmentServices : IAppointmentServices
    {
        public const int SlotMinutes = 30;
        public const int MaxUpcomingAppointments = 3;
        public const int MaxAppointmentsPerDaySession = 1;
        public const int ReschedulesPerDay = 3;
        public const int CancelsBeforeBookingCloses = 3;

        private static readonly TimeSpan MalaysiaOffset = TimeSpan.FromHours(8);
        private readonly AppDbContext _db;
        private readonly ICounterServices _counters;

        public AppointmentServices(AppDbContext db, ICounterServices counters)
        {
            _db = db;
            _counters = counters;
        }

        public async Task<ApiResponse<BookingCatalogResponse>> GetCatalogAsync(long userId)
        {
            var patient = await FindPatientAsync(userId);
            if (patient == null)
            {
                return ApiResponse<BookingCatalogResponse>.Fail("This account is not a patient account.", 400);
            }

            var doctors = await ListActiveDoctorsAsync();
            var types = await _db.VisitTypes
                .AsNoTracking()
                .Where(type => type.IsActive)
                .OrderBy(type => type.VisitTypeId)
                .Select(type => new BookingVisitTypeResponse
                {
                    Id = type.VisitTypeId,
                    Name = type.Name,
                    Description = type.Description,
                })
                .ToListAsync();
            var today = DateTimeHelper.MalaysiaNow().Date;
            var cancelsToday = await CountCancelsTodayAsync(patient.PatientId, today);

            return ApiResponse<BookingCatalogResponse>.Ok(new BookingCatalogResponse
            {
                Doctors = doctors,
                Types = types,
                BookingClosed = cancelsToday > CancelsBeforeBookingCloses,
                CancelsToday = cancelsToday,
                CancelLimit = CancelsBeforeBookingCloses,
                RescheduleLimit = ReschedulesPerDay,
            });
        }

        public async Task<ApiResponse<BookingSlotsResponse>> GetSlotsAsync(long userId, string date, string doctorId)
        {
            if (await FindPatientAsync(userId) == null)
            {
                return ApiResponse<BookingSlotsResponse>.Fail("This account is not a patient account.", 400);
            }

            if (!TryParseDate(date, out var day))
            {
                return ApiResponse<BookingSlotsResponse>.Fail("Pick a valid date.", 400);
            }

            var doctorKey = (doctorId ?? "").Trim();
            if (doctorKey.Length == 0)
            {
                return ApiResponse<BookingSlotsResponse>.Fail("Pick a doctor.", 400);
            }

            var now = DateTimeHelper.MalaysiaNow();
            var slots = await BuildSlotsAsync(day, doctorKey, now);
            return ApiResponse<BookingSlotsResponse>.Ok(slots);
        }

        public async Task<ApiResponse<List<AppointmentResponse>>> ListUpcomingAsync(long userId)
        {
            var patient = await FindPatientAsync(userId);
            if (patient == null)
            {
                return ApiResponse<List<AppointmentResponse>>.Fail("This account is not a patient account.", 400);
            }

            var now = DateTimeHelper.MalaysiaNow();
            var rows = await LoadPatientAppointmentsAsync(patient.PatientId);
            var upcoming = rows
                .Where(row => row.Status == "Scheduled" && ToStart(row) > now)
                .OrderBy(row => ToStart(row))
                .Select(ToResponse)
                .ToList();
            return ApiResponse<List<AppointmentResponse>>.Ok(upcoming);
        }

        public async Task<ApiResponse<List<AppointmentResponse>>> ListHistoryAsync(long userId)
        {
            var patient = await FindPatientAsync(userId);
            if (patient == null)
            {
                return ApiResponse<List<AppointmentResponse>>.Fail("This account is not a patient account.", 400);
            }

            var now = DateTimeHelper.MalaysiaNow();
            var rows = await LoadPatientAppointmentsAsync(patient.PatientId);
            var history = rows
                .Where(row => row.Status != "Scheduled" || ToStart(row) <= now)
                .OrderByDescending(row => ToStart(row))
                .Select(ToResponse)
                .ToList();
            return ApiResponse<List<AppointmentResponse>>.Ok(history);
        }

        public async Task<ApiResponse<AppointmentResponse>> BookAsync(long userId, AppointmentBookRequest request)
        {
            var patient = await FindPatientAsync(userId);
            if (patient == null)
            {
                return ApiResponse<AppointmentResponse>.Fail("This account is not a patient account.", 400);
            }

            var today = DateTimeHelper.MalaysiaNow().Date;
            if (await CountCancelsTodayAsync(patient.PatientId, today) > CancelsBeforeBookingCloses)
            {
                return ApiResponse<AppointmentResponse>.Fail("booking-closed", 400);
            }

            if (!TryParseStart(request.StartAt, out var start))
            {
                return ApiResponse<AppointmentResponse>.Fail("Pick a valid time slot.", 400);
            }

            if (await CountUpcomingAppointmentsAsync(patient.PatientId, DateTimeHelper.MalaysiaNow()) >= MaxUpcomingAppointments)
            {
                return ApiResponse<AppointmentResponse>.Fail("upcoming-limit", 400);
            }

            if (await HasAppointmentInDaySessionAsync(patient.PatientId, start, null))
            {
                return ApiResponse<AppointmentResponse>.Fail("same-day-session-limit", 400);
            }

            var type = (request.Type ?? "").Trim().ToLowerInvariant();
            if (!await _db.VisitTypes.AsNoTracking().AnyAsync(item => item.VisitTypeId == type && item.IsActive))
            {
                return ApiResponse<AppointmentResponse>.Fail("Pick a visit type.", 400);
            }

            var notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim();
            if (notes != null && notes.Length > 500)
            {
                return ApiResponse<AppointmentResponse>.Fail("Keep the notes under 500 characters.", 400);
            }

            var forName = string.IsNullOrWhiteSpace(request.ForName)
                ? (patient.User?.Name ?? patient.PatientId)
                : request.ForName.Trim();
            if (forName.Length > 100)
            {
                return ApiResponse<AppointmentResponse>.Fail("The patient name is too long.", 400);
            }

            var day = start.Date;
            var time = start.TimeOfDay;
            var doctorId = (request.DoctorId ?? "").Trim();
            if (doctorId.Equals("any", StringComparison.OrdinalIgnoreCase))
            {
                var assigned = await FindOpenDoctorAsync(day, time, DateTimeHelper.MalaysiaNow());
                if (assigned == null)
                {
                    return ApiResponse<AppointmentResponse>.Fail("That slot is already booked.", 400);
                }

                doctorId = assigned;
            }
            else if (!await DoctorIsOpenAsync(doctorId, day, time, DateTimeHelper.MalaysiaNow()))
            {
                return ApiResponse<AppointmentResponse>.Fail("That slot is already booked.", 400);
            }

            var appointmentId = await _counters.GenerateNextIdAsync("AppointmentId");
            var row = new Appointment
            {
                AppointmentId = appointmentId,
                PatientId = patient.PatientId,
                DoctorId = doctorId,
                AppointmentDate = day,
                AppointmentTime = time,
                ForName = forName,
                VisitType = type,
                Notes = notes,
                RescheduleCount = 0,
                Status = "Scheduled",
                CheckInStatus = "Pending",
            };
            _db.Appointments.Add(row);
            await _db.SaveChangesAsync();

            var saved = await LoadAppointmentAsync(appointmentId);
            return ApiResponse<AppointmentResponse>.Ok(ToResponse(saved!));
        }

        public async Task<ApiResponse<AppointmentResponse>> CancelAsync(long userId, string appointmentId)
        {
            var patient = await FindPatientAsync(userId);
            if (patient == null)
            {
                return ApiResponse<AppointmentResponse>.Fail("This account is not a patient account.", 400);
            }

            var row = await _db.Appointments.FirstOrDefaultAsync(item =>
                item.AppointmentId == appointmentId && item.PatientId == patient.PatientId);
            if (row == null || row.Status != "Scheduled" || ToStart(row) <= DateTimeHelper.MalaysiaNow())
            {
                return ApiResponse<AppointmentResponse>.Fail("Visit not found.", 404);
            }

            row.Status = "Cancelled";
            row.CancelledAt = DateTimeHelper.MalaysiaNow();
            await _db.SaveChangesAsync();
            var saved = await LoadAppointmentAsync(row.AppointmentId);
            return ApiResponse<AppointmentResponse>.Ok(ToResponse(saved!));
        }

        public async Task<ApiResponse<AppointmentResponse>> RescheduleAsync(long userId, string appointmentId, AppointmentRescheduleRequest request)
        {
            var patient = await FindPatientAsync(userId);
            if (patient == null)
            {
                return ApiResponse<AppointmentResponse>.Fail("This account is not a patient account.", 400);
            }

            var row = await _db.Appointments.FirstOrDefaultAsync(item =>
                item.AppointmentId == appointmentId && item.PatientId == patient.PatientId);
            if (row == null || row.Status != "Scheduled" || ToStart(row) <= DateTimeHelper.MalaysiaNow())
            {
                return ApiResponse<AppointmentResponse>.Fail("Visit not found.", 404);
            }

            if (!TryParseStart(request.StartAt, out var start))
            {
                return ApiResponse<AppointmentResponse>.Fail("Pick a valid time slot.", 400);
            }

            if (start == ToStart(row))
            {
                var same = await LoadAppointmentAsync(row.AppointmentId);
                return ApiResponse<AppointmentResponse>.Ok(ToResponse(same!));
            }

            var now = DateTimeHelper.MalaysiaNow();
            if (row.LastRescheduledAt?.Date != now.Date)
            {
                row.RescheduleCount = 0;
            }

            if (row.RescheduleCount >= ReschedulesPerDay)
            {
                return ApiResponse<AppointmentResponse>.Fail("reschedule-limit", 400);
            }

            if (await HasAppointmentInDaySessionAsync(patient.PatientId, start, row.AppointmentId))
            {
                return ApiResponse<AppointmentResponse>.Fail("same-day-session-limit", 400);
            }

            if (!await DoctorIsOpenAsync(row.DoctorId, start.Date, start.TimeOfDay, DateTimeHelper.MalaysiaNow(), row.AppointmentId))
            {
                return ApiResponse<AppointmentResponse>.Fail("That slot is already booked.", 400);
            }

            row.AppointmentDate = start.Date;
            row.AppointmentTime = start.TimeOfDay;
            row.RescheduleCount += 1;
            row.LastRescheduledAt = now;
            await _db.SaveChangesAsync();

            var saved = await LoadAppointmentAsync(row.AppointmentId);
            return ApiResponse<AppointmentResponse>.Ok(ToResponse(saved!));
        }

        private async Task<Models.Patient.Patient?> FindPatientAsync(long userId)
        {
            return await _db.Patients
                .Include(row => row.User)
                .FirstOrDefaultAsync(row => row.UserId == userId);
        }

        private async Task<List<BookingDoctorResponse>> ListActiveDoctorsAsync()
        {
            var rows = await _db.Doctors
                .AsNoTracking()
                .Include(row => row.User)
                .Where(row => row.User.Status == "Active")
                .OrderBy(row => row.User.Name)
                .ToListAsync();

            return rows.Select(row => new BookingDoctorResponse
            {
                Id = row.StaffId,
                Name = FormatDoctorName(row.User.Name),
                Specialty = row.Specialization,
                Bio = $"{row.Qualification}. {row.YearsOfExperience} years of experience.",
                Qualification = row.Qualification,
                YearsOfExperience = row.YearsOfExperience,
            }).ToList();
        }

        private async Task<List<Appointment>> LoadPatientAppointmentsAsync(string patientId)
        {
            return await _db.Appointments
                .AsNoTracking()
                .Include(row => row.Patient)
                    .ThenInclude(patient => patient.User)
                .Include(row => row.Doctor)
                    .ThenInclude(doctor => doctor.User)
                .Where(row => row.PatientId == patientId)
                .ToListAsync();
        }

        private async Task<Appointment?> LoadAppointmentAsync(string appointmentId)
        {
            return await _db.Appointments
                .AsNoTracking()
                .Include(row => row.Patient)
                    .ThenInclude(patient => patient.User)
                .Include(row => row.Doctor)
                    .ThenInclude(doctor => doctor.User)
                .FirstOrDefaultAsync(row => row.AppointmentId == appointmentId);
        }

        private async Task<int> CountCancelsTodayAsync(string patientId, DateTime today)
        {
            var dayEnd = today.AddDays(1);
            return await _db.Appointments.AsNoTracking()
                .CountAsync(row =>
                    row.PatientId == patientId
                    && row.Status == "Cancelled"
                    && row.CancelledAt != null
                    && row.CancelledAt >= today
                    && row.CancelledAt < dayEnd);
        }

        private async Task<int> CountUpcomingAppointmentsAsync(string patientId, DateTime now)
        {
            var rows = await _db.Appointments.AsNoTracking()
                .Where(row => row.PatientId == patientId && row.Status == "Scheduled")
                .Select(row => new { row.AppointmentDate, row.AppointmentTime })
                .ToListAsync();

            return rows.Count(row => row.AppointmentDate.Date + row.AppointmentTime > now);
        }

        private async Task<bool> HasAppointmentInDaySessionAsync(string patientId, DateTime start, string? ignoreAppointmentId)
        {
            var dayEnd = start.Date.AddDays(1);
            var rows = await _db.Appointments.AsNoTracking()
                .Where(row =>
                    row.PatientId == patientId
                    && row.Status == "Scheduled"
                    && row.AppointmentDate >= start.Date
                    && row.AppointmentDate < dayEnd
                    && (ignoreAppointmentId == null || row.AppointmentId != ignoreAppointmentId))
                .Select(row => row.AppointmentTime)
                .ToListAsync();

            var requestedIsNight = IsNightSession(start.TimeOfDay);
            return rows.Count(time => IsNightSession(time) == requestedIsNight) >= MaxAppointmentsPerDaySession;
        }

        private static bool IsNightSession(TimeSpan time) => time.Hours >= 18 || time.Hours < 6;

        private async Task<BookingSlotsResponse> BuildSlotsAsync(DateTime day, string doctorId, DateTime now)
        {
            var morning = new List<BookingSlotResponse>();
            var night = new List<BookingSlotResponse>();
            var doctorIds = doctorId.Equals("any", StringComparison.OrdinalIgnoreCase)
                ? await _db.Doctors.AsNoTracking().Select(row => row.StaffId).ToListAsync()
                : [doctorId];

            if (doctorIds.Count == 0)
            {
                return new BookingSlotsResponse { Date = day.ToString("yyyy-MM-dd"), DoctorId = doctorId };
            }

            var onLeave = await StaffOnLeaveAsync(doctorIds, day);
            var activeIds = doctorIds.Where(id => !onLeave.Contains(id)).ToList();
            if (activeIds.Count == 0)
            {
                return new BookingSlotsResponse { Date = day.ToString("yyyy-MM-dd"), DoctorId = doctorId };
            }

            var schedules = await _db.WorkSchedules.AsNoTracking()
                .Where(row => activeIds.Contains(row.StaffId) && row.WorkingDate == day && row.Status == "Active")
                .ToListAsync();

            var taken = await _db.Appointments.AsNoTracking()
                .Where(row => activeIds.Contains(row.DoctorId) && row.AppointmentDate == day && row.Status == "Scheduled")
                .Select(row => new { row.DoctorId, row.AppointmentTime })
                .ToListAsync();

            var openTimes = new SortedSet<TimeSpan>();
            foreach (var schedule in schedules)
            {
                for (var cursor = schedule.StartTime; cursor + TimeSpan.FromMinutes(SlotMinutes) <= schedule.EndTime; cursor += TimeSpan.FromMinutes(SlotMinutes))
                {
                    openTimes.Add(cursor);
                }
            }

            foreach (var time in openTimes)
            {
                var start = day + time;
                var startAt = ToOffset(start).ToString("yyyy-MM-ddTHH:mm:sszzz");
                var label = ToOffset(start).ToString("h:mm tt", CultureInfo.GetCultureInfo("en-MY"));
                var freeDoctor = activeIds.Any(id =>
                    schedules.Any(schedule => schedule.StaffId == id && time >= schedule.StartTime && time + TimeSpan.FromMinutes(SlotMinutes) <= schedule.EndTime)
                    && !taken.Any(row => row.DoctorId == id && row.AppointmentTime == time));
                var booked = start <= now || !freeDoctor;
                var item = new BookingSlotResponse { StartAt = startAt, Label = label, Booked = booked };
                if (time.Hours < 18 && time.Hours >= 6)
                {
                    morning.Add(item);
                }
                else
                {
                    night.Add(item);
                }
            }

            return new BookingSlotsResponse
            {
                Date = day.ToString("yyyy-MM-dd"),
                DoctorId = doctorId,
                Morning = morning,
                Night = night,
            };
        }

        private async Task<string?> FindOpenDoctorAsync(DateTime day, TimeSpan time, DateTime now)
        {
            var doctors = await _db.Doctors.AsNoTracking().Select(row => row.StaffId).ToListAsync();
            foreach (var doctorId in doctors)
            {
                if (await DoctorIsOpenAsync(doctorId, day, time, now))
                {
                    return doctorId;
                }
            }

            return null;
        }

        private async Task<bool> DoctorIsOpenAsync(string doctorId, DateTime day, TimeSpan time, DateTime now, string? ignoreAppointmentId = null)
        {
            if (day + time <= now)
            {
                return false;
            }

            if ((await StaffOnLeaveAsync([doctorId], day)).Contains(doctorId))
            {
                return false;
            }

            var onDuty = await _db.WorkSchedules.AsNoTracking().AnyAsync(row =>
                row.StaffId == doctorId
                && row.WorkingDate == day
                && row.Status == "Active"
                && time >= row.StartTime
                && time + TimeSpan.FromMinutes(SlotMinutes) <= row.EndTime);
            if (!onDuty)
            {
                return false;
            }

            var taken = await _db.Appointments.AsNoTracking().AnyAsync(row =>
                row.DoctorId == doctorId
                && row.AppointmentDate == day
                && row.AppointmentTime == time
                && row.Status == "Scheduled"
                && (ignoreAppointmentId == null || row.AppointmentId != ignoreAppointmentId));
            return !taken;
        }

        private async Task<HashSet<string>> StaffOnLeaveAsync(IEnumerable<string> staffIds, DateTime day)
        {
            var ids = staffIds.ToList();
            if (ids.Count == 0)
            {
                return [];
            }

            var dayStart = day;
            var dayEnd = day.AddDays(1);
            var rows = await _db.LeaveApplications.AsNoTracking()
                .Where(row =>
                    ids.Contains(row.StaffId)
                    && row.Status == "Approved"
                    && row.StartDate < dayEnd
                    && row.EndDate >= dayStart)
                .Select(row => row.StaffId)
                .ToListAsync();
            return rows.ToHashSet(StringComparer.OrdinalIgnoreCase);
        }

        private static AppointmentResponse ToResponse(Appointment row)
        {
            var start = ToStart(row);
            var end = start.AddMinutes(SlotMinutes);
            var status = row.Status switch
            {
                "Cancelled" => "cancelled",
                "Completed" => "completed",
                _ => "confirmed",
            };
            return new AppointmentResponse
            {
                Id = row.AppointmentId,
                ForName = string.IsNullOrWhiteSpace(row.ForName) ? (row.Patient?.User?.Name ?? row.PatientId) : row.ForName,
                DoctorId = row.DoctorId,
                DoctorName = FormatDoctorName(row.Doctor?.User?.Name),
                Type = row.VisitType,
                Notes = row.Notes,
                StartAt = ToOffset(start).ToString("yyyy-MM-ddTHH:mm:sszzz"),
                EndAt = ToOffset(end).ToString("yyyy-MM-ddTHH:mm:sszzz"),
                Status = status,
                RescheduleCount = row.RescheduleCount,
                RescheduleBlocked = row.LastRescheduledAt?.Date == DateTimeHelper.MalaysiaNow().Date && row.RescheduleCount >= ReschedulesPerDay,
            };
        }

        private static DateTime ToStart(Appointment row) => row.AppointmentDate.Date + row.AppointmentTime;

        private static DateTimeOffset ToOffset(DateTime malaysiaLocal) =>
            new(DateTime.SpecifyKind(malaysiaLocal, DateTimeKind.Unspecified), MalaysiaOffset);

        private static string FormatDoctorName(string? name)
        {
            var value = (name ?? "").Trim();
            if (value.Length == 0)
            {
                return "Doctor";
            }

            return value.StartsWith("Dr ", StringComparison.OrdinalIgnoreCase) ? value : $"Dr {value}";
        }

        private static bool TryParseDate(string? value, out DateTime day)
        {
            day = default;
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            if (DateTime.TryParseExact(value.Trim(), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
            {
                day = parsed.Date;
                return true;
            }

            return false;
        }

        private static bool TryParseStart(string? value, out DateTime start)
        {
            start = default;
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            if (!DateTimeOffset.TryParse(value.Trim(), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var parsed))
            {
                return false;
            }

            start = TimeZoneInfo.ConvertTime(parsed, TimeZoneInfo.FindSystemTimeZoneById("Singapore Standard Time")).DateTime;
            start = new DateTime(start.Year, start.Month, start.Day, start.Hour, start.Minute, 0);
            return true;
        }
    }
}
