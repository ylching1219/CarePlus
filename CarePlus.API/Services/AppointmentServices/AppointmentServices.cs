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
        public const int MinimumChildSelfBookingAge = 12;

        private static readonly TimeSpan MalaysiaOffset = TimeSpan.FromHours(8);
        private static readonly TimeSpan MorningStart = TimeSpan.FromHours(6);
        private static readonly TimeSpan MorningEnd = TimeSpan.FromHours(18);
        private static readonly TimeSpan NightStart = TimeSpan.FromHours(18);
        private static readonly TimeSpan NightEnd = TimeSpan.FromHours(6);
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
                    Price = type.Price,
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

        public async Task<ApiResponse<List<string>>> GetAvailableDatesAsync(long userId, string doctorId, int days)
        {
            if (await FindPatientAsync(userId) == null)
            {
                return ApiResponse<List<string>>.Fail("This account is not a patient account.", 400);
            }

            var doctorKey = (doctorId ?? "any").Trim();
            if (doctorKey.Length == 0)
            {
                return ApiResponse<List<string>>.Fail("Pick a doctor.", 400);
            }

            var rangeDays = Math.Clamp(days, 7, 180);
            var today = DateOnly.FromDateTime(DateTimeHelper.MalaysiaNow());
            var lastDay = today.AddDays(rangeDays);
            var schedules = _db.WorkSchedules
                .AsNoTracking()
                .Where(row => row.Status == "Active" && row.WorkingDate >= today && row.WorkingDate <= lastDay);

            if (!doctorKey.Equals("any", StringComparison.OrdinalIgnoreCase))
            {
                schedules = schedules.Where(row => row.StaffId == doctorKey);
            }

            var dates = await schedules
                .Where(schedule => _db.RoomAssignments.Any(assignment =>
                    assignment.ScheduleId == schedule.ScheduleId
                    || (assignment.DoctorId == schedule.StaffId
                        && assignment.AssignmentDate == schedule.WorkingDate)))
                .Select(row => row.WorkingDate)
                .Distinct()
                .OrderBy(day => day)
                .ToListAsync();

            return ApiResponse<List<string>>.Ok(dates.Select(day => day.ToString("yyyy-MM-dd")).ToList());
        }

        public async Task<ApiResponse<List<BookingDoctorResponse>>> GetAvailableDoctorsAsync(
            string? date,
            string? time)
        {
            var now = DateTimeHelper.MalaysiaNow();

            var day = now.Date;
            if (!string.IsNullOrWhiteSpace(date)
                && !TryParseDate(date, out day))
            {
                return ApiResponse<List<BookingDoctorResponse>>.Fail(
                    "Pick a valid appointment date.", 400);
            }

            var appointmentTime = GetNextSlotTime(now.TimeOfDay);
            if (!string.IsNullOrWhiteSpace(time)
                && !TimeSpan.TryParseExact(
                    time.Trim(),
                    @"hh\:mm",
                    CultureInfo.InvariantCulture,
                    out appointmentTime))
            {
                return ApiResponse<List<BookingDoctorResponse>>.Fail(
                    "Pick a valid appointment time.", 400);
            }

            var activeDoctors = await ListActiveDoctorsAsync();
            var availableDoctors = new List<BookingDoctorResponse>();

            foreach (var doctor in activeDoctors)
            {
                if (await DoctorIsOpenAsync(doctor.Id, day, appointmentTime, now))
                {
                    availableDoctors.Add(doctor);
                }
            }

            return ApiResponse<List<BookingDoctorResponse>>.Ok(availableDoctors);
        }

        public async Task<ApiResponse<List<AppointmentResponse>>> ListUpcomingAsync(long userId)
        {
            var patient = await FindPatientAsync(userId);
            if (patient == null)
            {
                return ApiResponse<List<AppointmentResponse>>.Fail("This account is not a patient account.", 400);
            }

            var now = DateTimeHelper.MalaysiaNow();
            var rows = await LoadVisibleAppointmentsAsync(patient.PatientId);
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
            var rows = await LoadVisibleAppointmentsAsync(patient.PatientId);
            var history = rows
                .Where(row => row.Status != "Scheduled" || ToStart(row) <= now)
                .OrderByDescending(row => ToStart(row))
                .Select(ToResponse)
                .ToList();
            return ApiResponse<List<AppointmentResponse>>.Ok(history);
        }

        public async Task<ApiResponse<List<StaffAppointmentResponse>>> ListStaffAsync(
            long userId,
            string role,
            string? date)
        {
            if (!string.Equals(role, "Admin", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(role, "Doctor", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(role, "Pharmacist", StringComparison.OrdinalIgnoreCase))
            {
                return ApiResponse<List<StaffAppointmentResponse>>.Fail(
                    "This account is not a staff account.", 403);
            }

            var query = _db.Appointments
                .AsNoTracking()
                .Include(row => row.Patient)
                    .ThenInclude(patient => patient.User)
                .Include(row => row.Doctor)
                    .ThenInclude(doctor => doctor.User)
                .Include(row => row.Room)
                .AsQueryable();

            if (string.Equals(role, "Doctor", StringComparison.OrdinalIgnoreCase))
            {
                var doctorId = await _db.Doctors
                    .AsNoTracking()
                    .Where(doctor => doctor.UserId == userId)
                    .Select(doctor => doctor.StaffId)
                    .FirstOrDefaultAsync();

                if (doctorId == null)
                {
                    return ApiResponse<List<StaffAppointmentResponse>>.Fail(
                        "Doctor profile was not found.", 404);
                }

                query = query.Where(row => row.DoctorId == doctorId);
            }

            if (!string.IsNullOrWhiteSpace(date))
            {
                if (!TryParseDate(date, out var selectedDate))
                {
                    return ApiResponse<List<StaffAppointmentResponse>>.Fail(
                        "Pick a valid appointment date.", 400);
                }

                var nextDate = selectedDate.Date.AddDays(1);
                query = query.Where(row =>
                    row.AppointmentDate >= selectedDate.Date
                    && row.AppointmentDate < nextDate);
            }

            var rows = await query
                .OrderBy(row => row.AppointmentDate)
                .ThenBy(row => row.AppointmentTime)
                .ToListAsync();

            var visitTypes = await _db.VisitTypes
                .AsNoTracking()
                .ToDictionaryAsync(type => type.VisitTypeId, type => type.Name);

            return ApiResponse<List<StaffAppointmentResponse>>.Ok(
                rows.Select(row => ToStaffResponse(row, visitTypes)).ToList());
        }

        public async Task<ApiResponse<AppointmentResponse>> BookAsync(long userId, AppointmentBookRequest request)
        {
            var patient = await FindPatientAsync(userId);
            if (patient == null)
            {
                return ApiResponse<AppointmentResponse>.Fail("This account is not a patient account.", 400);
            }

            var now = DateTimeHelper.MalaysiaNow();
            var today = now.Date;
            var currentMembership = await _db.FamilyMembers
                .AsNoTracking()
                .FirstOrDefaultAsync(row => row.PatientId == patient.PatientId && row.Status == "Active");
            var isChild = currentMembership != null
                && string.Equals(currentMembership.Relationship, "child", StringComparison.OrdinalIgnoreCase);
            var age = CalculateAge(patient.DateOfBirth, today);

            if (isChild && age == null)
            {
                return ApiResponse<AppointmentResponse>.Fail("age-verification-required", 403);
            }

            if (isChild && age < MinimumChildSelfBookingAge)
            {
                return ApiResponse<AppointmentResponse>.Fail("child-underage-booking", 403);
            }

            var bookingPatient = patient;
            var requestedPatientId = (request.PatientId ?? "").Trim();
            if (!string.IsNullOrWhiteSpace(requestedPatientId)
                && !string.Equals(requestedPatientId, patient.PatientId, StringComparison.OrdinalIgnoreCase))
            {
                if (currentMembership == null)
                {
                    return ApiResponse<AppointmentResponse>.Fail("You do not belong to an active family.", 403);
                }

                if (isChild)
                {
                    return ApiResponse<AppointmentResponse>.Fail("child-self-only", 403);
                }

                bookingPatient = await _db.FamilyMembers
                    .AsNoTracking()
                    .Include(row => row.Patient)
                        .ThenInclude(row => row.User)
                    .Where(row =>
                        row.FamilyGroupId == currentMembership.FamilyGroupId
                        && row.PatientId == requestedPatientId
                        && row.Status == "Active")
                    .Select(row => row.Patient)
                    .FirstOrDefaultAsync();
                if (bookingPatient == null)
                {
                    return ApiResponse<AppointmentResponse>.Fail("You can only book for an active member of your family.", 403);
                }
            }

            if (await CountCancelsTodayAsync(bookingPatient.PatientId, today) > CancelsBeforeBookingCloses)
            {
                return ApiResponse<AppointmentResponse>.Fail("booking-closed", 400);
            }

            if (!TryParseStart(request.StartAt, out var start))
            {
                return ApiResponse<AppointmentResponse>.Fail("Pick a valid time slot.", 400);
            }

            if (start < now.AddMinutes(30))
            {
                return ApiResponse<AppointmentResponse>.Fail("slot-too-soon", 400);
            }

            if (await CountUpcomingAppointmentsAsync(bookingPatient.PatientId, now) >= MaxUpcomingAppointments)
            {
                return ApiResponse<AppointmentResponse>.Fail("upcoming-limit", 400);
            }

            if (await HasAppointmentInDaySessionAsync(bookingPatient.PatientId, start, null))
            {
                return ApiResponse<AppointmentResponse>.Fail("same-day-session-limit", 400);
            }

            var requestedType = (request.Type ?? "").Trim();
            var normalizedType = requestedType.ToUpperInvariant();
            var visitType = await _db.VisitTypes
                .AsNoTracking()
                .FirstOrDefaultAsync(item =>
                    item.IsActive
                    && (item.VisitTypeId.ToUpper() == normalizedType
                        || item.Name.ToUpper() == normalizedType));

            if (visitType == null)
            {
                return ApiResponse<AppointmentResponse>.Fail("Pick a visit type.", 400);
            }

            var type = visitType.VisitTypeId;

            var notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim();
            if (notes != null && notes.Length > 500)
            {
                return ApiResponse<AppointmentResponse>.Fail("Keep the notes under 500 characters.", 400);
            }

            var forName = string.IsNullOrWhiteSpace(request.ForName)
                ? (bookingPatient.User?.Name ?? bookingPatient.PatientId)
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
                var assigned = await FindOpenDoctorAsync(day, time, now);
                if (assigned == null)
                {
                    return ApiResponse<AppointmentResponse>.Fail("That slot is already booked.", 400);
                }

                doctorId = assigned;
            }
            else if (!await DoctorIsOpenAsync(doctorId, day, time, now))
            {
                return ApiResponse<AppointmentResponse>.Fail("That slot is already booked.", 400);
            }

            var roomId = await FindRoomIdForSlotAsync(doctorId, day, time);
            if (roomId == null)
            {
                return ApiResponse<AppointmentResponse>.Fail(
                    "No available clinic room is assigned to this doctor at that time.", 409);
            }

            var appointmentId = await _counters.GenerateNextIdAsync("AppointmentId");
            var row = new Appointment
            {
                AppointmentId = appointmentId,
                PatientId = bookingPatient.PatientId,
                DoctorId = doctorId,
                RoomId = roomId,
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

        public async Task<ApiResponse<AppointmentResponse>> BookForStaffAsync(
            StaffAppointmentBookRequest request)
        {
            var patientId = (request.PatientId ?? "").Trim();
            var patient = await _db.Patients
                .Include(row => row.User)
                .FirstOrDefaultAsync(row => row.PatientId == patientId);
            if (patient == null || !string.Equals(patient.User?.Status, "Active", StringComparison.OrdinalIgnoreCase))
            {
                return ApiResponse<AppointmentResponse>.Fail("Active patient not found.", 404);
            }

            if (!TryParseStart(request.StartAt, out var start))
            {
                return ApiResponse<AppointmentResponse>.Fail("Pick a valid time slot.", 400);
            }

            var now = DateTimeHelper.MalaysiaNow();
            if (start < now.AddMinutes(30))
            {
                return ApiResponse<AppointmentResponse>.Fail(
                    "Appointments must be booked at least 30 minutes in advance.", 400);
            }

            var doctorId = (request.DoctorId ?? "").Trim();
            var activeDoctor = await _db.Doctors
                .AsNoTracking()
                .Include(row => row.User)
                .AnyAsync(row =>
                    row.StaffId == doctorId
                    && row.User.Status == "Active");
            if (!activeDoctor)
            {
                return ApiResponse<AppointmentResponse>.Fail(
                    "Active doctor not found.", 404);
            }

            var requestedType = (request.Type ?? "").Trim();
            var normalizedType = requestedType.ToUpperInvariant();
            var visitType = await _db.VisitTypes
                .AsNoTracking()
                .FirstOrDefaultAsync(item =>
                    item.IsActive
                    && (item.VisitTypeId.ToUpper() == normalizedType
                        || item.Name.ToUpper() == normalizedType));
            if (visitType == null)
            {
                return ApiResponse<AppointmentResponse>.Fail("Pick a visit type.", 400);
            }

            var notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim();
            if (notes != null && notes.Length > 500)
            {
                return ApiResponse<AppointmentResponse>.Fail("Keep the notes under 500 characters.", 400);
            }

            if (await HasAppointmentInDaySessionAsync(patient.PatientId, start, null))
            {
                return ApiResponse<AppointmentResponse>.Fail("same-day-session-limit", 400);
            }

            if (!await DoctorIsOpenAsync(doctorId, start.Date, start.TimeOfDay, now))
            {
                return ApiResponse<AppointmentResponse>.Fail("That slot is already booked.", 400);
            }

            var roomId = await FindRoomIdForSlotAsync(doctorId, start.Date, start.TimeOfDay);
            if (roomId == null)
            {
                return ApiResponse<AppointmentResponse>.Fail(
                    "No available clinic room is assigned to this doctor at that time.", 409);
            }

            var appointmentId = await _counters.GenerateNextIdAsync("AppointmentId");
            _db.Appointments.Add(new Appointment
            {
                AppointmentId = appointmentId,
                PatientId = patient.PatientId,
                DoctorId = doctorId,
                RoomId = roomId,
                AppointmentDate = start.Date,
                AppointmentTime = start.TimeOfDay,
                ForName = patient.User?.Name ?? patient.PatientId,
                VisitType = visitType.VisitTypeId,
                Notes = notes,
                RescheduleCount = 0,
                Status = "Scheduled",
                CheckInStatus = "Pending",
            });
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

            var row = await FindManageableAppointmentAsync(patient.PatientId, appointmentId);
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

            var row = await FindManageableAppointmentAsync(patient.PatientId, appointmentId);
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
            if (start < now.AddMinutes(30))
            {
                return ApiResponse<AppointmentResponse>.Fail("slot-too-soon", 400);
            }

            if (row.LastRescheduledAt?.Date != now.Date)
            {
                row.RescheduleCount = 0;
            }

            if (row.RescheduleCount >= ReschedulesPerDay)
            {
                return ApiResponse<AppointmentResponse>.Fail("reschedule-limit", 400);
            }

            // Conflict check is for the patient on the visit, not the parent who is managing it.
            if (await HasAppointmentInDaySessionAsync(row.PatientId, start, row.AppointmentId))
            {
                return ApiResponse<AppointmentResponse>.Fail("same-day-session-limit", 400);
            }

            if (!await DoctorIsOpenAsync(row.DoctorId, start.Date, start.TimeOfDay, DateTimeHelper.MalaysiaNow(), row.AppointmentId))
            {
                return ApiResponse<AppointmentResponse>.Fail("That slot is already booked.", 400);
            }

            var roomId = await FindRoomIdForSlotAsync(row.DoctorId, start.Date, start.TimeOfDay);
            if (roomId == null)
            {
                return ApiResponse<AppointmentResponse>.Fail(
                    "No available clinic room is assigned to this doctor at that time.", 409);
            }

            row.AppointmentDate = start.Date;
            row.AppointmentTime = start.TimeOfDay;
            row.RoomId = roomId;
            row.RescheduleCount += 1;
            row.LastRescheduledAt = now;
            await _db.SaveChangesAsync();

            var saved = await LoadAppointmentAsync(row.AppointmentId);
            return ApiResponse<AppointmentResponse>.Ok(ToResponse(saved!));
        }

        public async Task<ApiResponse<AppointmentResponse>> CancelForStaffAsync(string appointmentId)
        {
            var row = await _db.Appointments.FirstOrDefaultAsync(item => item.AppointmentId == appointmentId);
            var now = DateTimeHelper.MalaysiaNow();
            if (row == null || row.Status != "Scheduled" || ToStart(row) <= now)
            {
                return ApiResponse<AppointmentResponse>.Fail("Appointment cannot be cancelled.", 400);
            }

            row.Status = "Cancelled";
            row.CancelledAt = now;
            await _db.SaveChangesAsync();

            var saved = await LoadAppointmentAsync(row.AppointmentId);
            return ApiResponse<AppointmentResponse>.Ok(ToResponse(saved!));
        }

        public async Task<ApiResponse<AppointmentResponse>> RescheduleForStaffAsync(
            string appointmentId,
            AppointmentRescheduleRequest request)
        {
            var row = await _db.Appointments.FirstOrDefaultAsync(item => item.AppointmentId == appointmentId);
            var now = DateTimeHelper.MalaysiaNow();
            if (row == null || row.Status != "Scheduled" || ToStart(row) <= now)
            {
                return ApiResponse<AppointmentResponse>.Fail("Appointment cannot be rescheduled.", 400);
            }

            if (!TryParseStart(request.StartAt, out var start))
            {
                return ApiResponse<AppointmentResponse>.Fail("Pick a valid time slot.", 400);
            }

            if (start < now.AddMinutes(30))
            {
                return ApiResponse<AppointmentResponse>.Fail(
                    "Appointments must be booked at least 30 minutes in advance.", 400);
            }

            if (start == ToStart(row))
            {
                var same = await LoadAppointmentAsync(row.AppointmentId);
                return ApiResponse<AppointmentResponse>.Ok(ToResponse(same!));
            }

            if (await HasAppointmentInDaySessionAsync(row.PatientId, start, row.AppointmentId))
            {
                return ApiResponse<AppointmentResponse>.Fail("same-day-session-limit", 400);
            }

            if (!await DoctorIsOpenAsync(row.DoctorId, start.Date, start.TimeOfDay, now, row.AppointmentId))
            {
                return ApiResponse<AppointmentResponse>.Fail("That slot is already booked.", 400);
            }

            var roomId = await FindRoomIdForSlotAsync(row.DoctorId, start.Date, start.TimeOfDay);
            if (roomId == null)
            {
                return ApiResponse<AppointmentResponse>.Fail(
                    "No available clinic room is assigned to this doctor at that time.", 409);
            }

            row.AppointmentDate = start.Date;
            row.AppointmentTime = start.TimeOfDay;
            row.RoomId = roomId;
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

        private async Task<List<Appointment>> LoadVisibleAppointmentsAsync(string patientId)
        {
            var patientIds = await VisiblePatientIdsAsync(patientId);
            return await _db.Appointments
                .AsNoTracking()
                .Include(row => row.Patient)
                    .ThenInclude(patient => patient.User)
                .Include(row => row.Doctor)
                    .ThenInclude(doctor => doctor.User)
                .Include(row => row.Room)
                .Where(row => patientIds.Contains(row.PatientId))
                .ToListAsync();
        }

        // Parents see every active family member's visits; children only see their own.
        private async Task<List<string>> VisiblePatientIdsAsync(string patientId)
        {
            var membership = await _db.FamilyMembers
                .AsNoTracking()
                .FirstOrDefaultAsync(row => row.PatientId == patientId && row.Status == "Active");
            if (membership == null
                || !string.Equals(membership.Relationship, "parent", StringComparison.OrdinalIgnoreCase))
            {
                return [patientId];
            }

            return await _db.FamilyMembers
                .AsNoTracking()
                .Where(row => row.FamilyGroupId == membership.FamilyGroupId && row.Status == "Active")
                .Select(row => row.PatientId)
                .Distinct()
                .ToListAsync();
        }

        private async Task<Appointment?> FindManageableAppointmentAsync(string actorPatientId, string appointmentId)
        {
            var row = await _db.Appointments.FirstOrDefaultAsync(item => item.AppointmentId == appointmentId);
            if (row == null)
            {
                return null;
            }

            var visibleIds = await VisiblePatientIdsAsync(actorPatientId);
            return visibleIds.Contains(row.PatientId) ? row : null;
        }

        private async Task<Appointment?> LoadAppointmentAsync(string appointmentId)
        {
            return await _db.Appointments
                .AsNoTracking()
                .Include(row => row.Patient)
                    .ThenInclude(patient => patient.User)
                .Include(row => row.Doctor)
                    .ThenInclude(doctor => doctor.User)
                .Include(row => row.Room)
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

            var scheduleDate = DateOnly.FromDateTime(day);
            var schedules = await _db.WorkSchedules.AsNoTracking()
                .Where(row => activeIds.Contains(row.StaffId) && row.WorkingDate == scheduleDate && row.Status == "Active")
                .ToListAsync();

            var taken = await _db.Appointments.AsNoTracking()
                .Where(row => activeIds.Contains(row.DoctorId) && row.AppointmentDate == day && row.Status == "Scheduled")
                .Select(row => new { row.DoctorId, row.AppointmentTime })
                .ToListAsync();

            var openTimes = new SortedSet<TimeSpan>();
            foreach (var schedule in schedules)
            {
                foreach (var cursor in ShiftTimes(schedule.Shift))
                {
                    openTimes.Add(cursor);
                }
            }

            foreach (var time in openTimes)
            {
                var start = day + time;
                var startAt = ToOffset(start).ToString("yyyy-MM-ddTHH:mm:sszzz");
                var label = ToOffset(start).ToString("h:mm tt", CultureInfo.GetCultureInfo("en-MY"));
                var freeDoctor = false;
                foreach (var id in activeIds)
                {
                    if (schedules.Any(schedule => schedule.StaffId == id && IsTimeWithinShift(time, schedule.Shift))
                        && !taken.Any(row => row.DoctorId == id && row.AppointmentTime == time)
                        && await FindRoomIdForSlotAsync(id, day, time) != null)
                    {
                        freeDoctor = true;
                        break;
                    }
                }
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

        private async Task<string?> FindRoomIdForSlotAsync(
            string doctorId,
            DateTime day,
            TimeSpan time)
        {
            var assignmentDate = DateOnly.FromDateTime(day);
            var candidateTime = TimeOnly.FromTimeSpan(time);
            var assignments = await _db.RoomAssignments
                .AsNoTracking()
                .Include(assignment => assignment.Room)
                .Include(assignment => assignment.Schedule)
                .Where(assignment => assignment.DoctorId == doctorId
                    && assignment.AssignmentDate == assignmentDate
                    && assignment.Schedule != null
                    && assignment.Schedule.Status == "Active")
                .ToListAsync();

            return assignments
                .Where(assignment => string.Equals(
                    assignment.Room.Status,
                    "Available",
                    StringComparison.OrdinalIgnoreCase))
                .Where(assignment => IsTimeWithinAssignment(
                    candidateTime,
                    assignment.StartTime,
                    assignment.EndTime))
                .Select(assignment => assignment.RoomId)
                .FirstOrDefault();
        }

        private static bool IsTimeWithinAssignment(
            TimeOnly time,
            TimeOnly start,
            TimeOnly end)
        {
            return start <= end
                ? time >= start && time < end
                : time >= start || time < end;
        }

        private async Task<bool> DoctorIsOpenAsync(string doctorId, DateTime day, TimeSpan time, DateTime now, string? ignoreAppointmentId = null)
        {
            if (day + time < now.AddMinutes(30))
            {
                return false;
            }

            if ((await StaffOnLeaveAsync([doctorId], day)).Contains(doctorId))
            {
                return false;
            }

            var scheduleDate = DateOnly.FromDateTime(day);
            var requiredShift = IsNightSession(time) ? "Night" : "Morning";
            var onDuty = await _db.WorkSchedules.AsNoTracking().AnyAsync(row =>
                row.StaffId == doctorId
                && row.WorkingDate == scheduleDate
                && row.Status == "Active"
                && row.Shift == requiredShift);
            if (!onDuty)
            {
                return false;
            }

            if (!IsTimeWithinShift(time, requiredShift))
            {
                return false;
            }

            if (await FindRoomIdForSlotAsync(doctorId, day, time) == null)
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

        private static (TimeSpan Start, TimeSpan End) ShiftBounds(string shift) =>
            shift.Equals("Night", StringComparison.OrdinalIgnoreCase)
                ? (NightStart, NightEnd)
                : (MorningStart, MorningEnd);

        private static IEnumerable<TimeSpan> ShiftTimes(string shift)
        {
            if (shift.Equals("Night", StringComparison.OrdinalIgnoreCase))
            {
                for (var cursor = NightStart; cursor < TimeSpan.FromDays(1); cursor += TimeSpan.FromMinutes(SlotMinutes))
                {
                    yield return cursor;
                }

                for (var cursor = TimeSpan.Zero; cursor + TimeSpan.FromMinutes(SlotMinutes) <= NightEnd; cursor += TimeSpan.FromMinutes(SlotMinutes))
                {
                    yield return cursor;
                }

                yield break;
            }

            for (var cursor = MorningStart; cursor + TimeSpan.FromMinutes(SlotMinutes) <= MorningEnd; cursor += TimeSpan.FromMinutes(SlotMinutes))
            {
                yield return cursor;
            }
        }

        private static bool IsTimeWithinShift(TimeSpan time, string shift)
        {
            var slotEnd = time + TimeSpan.FromMinutes(SlotMinutes);
            if (shift.Equals("Night", StringComparison.OrdinalIgnoreCase))
            {
                return (time >= NightStart && slotEnd <= TimeSpan.FromDays(1))
                    || (time < NightEnd && slotEnd <= NightEnd);
            }

            var (start, end) = ShiftBounds(shift);
            return time >= start && slotEnd <= end;
        }

        private static TimeSpan GetNextSlotTime(TimeSpan currentTime)
        {
            var totalMinutes = Math.Ceiling(currentTime.TotalMinutes / SlotMinutes)
                * SlotMinutes;

            return totalMinutes >= TimeSpan.FromDays(1).TotalMinutes
                ? TimeSpan.Zero
                : TimeSpan.FromMinutes(totalMinutes);
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
                RoomId = row.RoomId,
                RoomName = row.Room?.RoomName,
                Type = row.VisitType,
                Notes = row.Notes,
                StartAt = ToOffset(start).ToString("yyyy-MM-ddTHH:mm:sszzz"),
                EndAt = ToOffset(end).ToString("yyyy-MM-ddTHH:mm:sszzz"),
                Status = status,
                RescheduleCount = row.RescheduleCount,
                RescheduleBlocked = row.LastRescheduledAt?.Date == DateTimeHelper.MalaysiaNow().Date && row.RescheduleCount >= ReschedulesPerDay,
            };
        }

        private static StaffAppointmentResponse ToStaffResponse(
            Appointment row,
            IReadOnlyDictionary<string, string> visitTypes)
        {
            var start = ToStart(row);
            var end = start.AddMinutes(SlotMinutes);

            return new StaffAppointmentResponse
            {
                Id = row.AppointmentId,
                PatientId = row.PatientId,
                PatientName = string.IsNullOrWhiteSpace(row.ForName)
                    ? (row.Patient?.User?.Name ?? row.PatientId)
                    : row.ForName,
                PatientUsername = row.Patient?.User?.Username ?? string.Empty,
                DoctorId = row.DoctorId,
                DoctorName = FormatDoctorName(row.Doctor?.User?.Name),
                RoomId = row.RoomId,
                RoomName = row.Room?.RoomName,
                VisitTypeId = row.VisitType,
                VisitType = visitTypes.GetValueOrDefault(row.VisitType, row.VisitType),
                Notes = row.Notes,
                StartAt = ToOffset(start).ToString("yyyy-MM-ddTHH:mm:sszzz"),
                EndAt = ToOffset(end).ToString("yyyy-MM-ddTHH:mm:sszzz"),
                Status = row.Status,
                CheckInStatus = row.CheckInStatus
            };
        }

        private static DateTime ToStart(Appointment row) => row.AppointmentDate.Date + row.AppointmentTime;

        private static DateTimeOffset ToOffset(DateTime malaysiaLocal) =>
            new(DateTime.SpecifyKind(malaysiaLocal, DateTimeKind.Unspecified), MalaysiaOffset);

        private static int? CalculateAge(DateTime? dateOfBirth, DateTime today)
        {
            if (!dateOfBirth.HasValue || dateOfBirth.Value.Date > today)
            {
                return null;
            }

            var age = today.Year - dateOfBirth.Value.Year;
            if (dateOfBirth.Value.Date > today.AddYears(-age))
            {
                age--;
            }

            return age;
        }

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
