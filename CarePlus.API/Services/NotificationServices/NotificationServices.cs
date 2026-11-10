using CarePlus.API.Data;
using CarePlus.API.DTO;
using CarePlus.API.DTO.AnnouncementDTO;
using CarePlus.API.DTO.NotificationDTO;
using CarePlus.API.Extension;
using CarePlus.API.Models.Notification;
using CarePlus.API.Models.Payment;
using CarePlus.API.Services.NotificationServices.Interface;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace CarePlus.API.Services.NotificationServices;

public class NotificationServices : INotificationServices
{
    private readonly AppDbContext _db;

    public NotificationServices(AppDbContext db)
    {
        _db = db;
    }

    public async Task<ApiResponse<List<AnnouncementResponse>>> ListPublishedAsync()
    {
        var rows = await _db.Notifications
            .AsNoTracking()
            .Where(row => row.UserId == null
                && row.Type == "announcement"
                && row.IsPublished
                && row.Status != "Deleted"
                && row.CreatedAt <= DateTimeHelper.MalaysiaNow())
            .OrderByDescending(row => row.CreatedAt)
            .ToListAsync();

        return ApiResponse<List<AnnouncementResponse>>.Ok(rows.Select(row => new AnnouncementResponse
        {
            Id = row.NotificationId,
            PostedOn = row.CreatedAt.ToString("yyyy-MM-dd"),
            Title = row.Title,
            Body = row.Message,
            TitleZh = row.TitleZh ?? row.Title,
            BodyZh = row.MessageZh ?? row.Message,
        }).ToList());
    }

    public async Task<ApiResponse<List<NotificationResponse>>> ListAsync(string patientId)
    {
        var patient = await _db.Patients.AsNoTracking().FirstOrDefaultAsync(row => row.PatientId == patientId);
        if (patient == null)
        {
            return ApiResponse<List<NotificationResponse>>.Fail("This account is not a patient account.", 403);
        }

        await SynchronizeAsync(patientId, patient.UserId);
        return await ReadResponseAsync(patient.UserId);
    }

    public async Task<ApiResponse<List<NotificationResponse>>> MarkReadAsync(string patientId, string notificationId)
    {
        var userId = await UserIdAsync(patientId);
        if (userId == null) return ApiResponse<List<NotificationResponse>>.Fail("This account is not a patient account.", 403);

        var row = await _db.Notifications.FirstOrDefaultAsync(item => item.UserId == userId && item.NotificationId == notificationId && item.Status != "Deleted");
        if (row == null) return ApiResponse<List<NotificationResponse>>.Fail("Notification was not found.", 404);
        row.Status = "Read";
        await _db.SaveChangesAsync();
        return await ReadResponseAsync(userId.Value);
    }

    public async Task<ApiResponse<List<NotificationResponse>>> MarkManyReadAsync(string patientId, List<string> notificationIds)
    {
        var userId = await UserIdAsync(patientId);
        if (userId == null) return ApiResponse<List<NotificationResponse>>.Fail("This account is not a patient account.", 403);

        var ids = CleanIds(notificationIds);
        if (ids.Count > 0)
        {
            var rows = await _db.Notifications.Where(item => item.UserId == userId && ids.Contains(item.NotificationId) && item.Status != "Deleted").ToListAsync();
            foreach (var row in rows) row.Status = "Read";
            await _db.SaveChangesAsync();
        }

        return await ReadResponseAsync(userId.Value);
    }

    public async Task<ApiResponse<List<NotificationResponse>>> MarkAllReadAsync(string patientId)
    {
        var userId = await UserIdAsync(patientId);
        if (userId == null) return ApiResponse<List<NotificationResponse>>.Fail("This account is not a patient account.", 403);

        var rows = await _db.Notifications.Where(item => item.UserId == userId && item.Status == "Unread").ToListAsync();
        foreach (var row in rows) row.Status = "Read";
        await _db.SaveChangesAsync();
        return await ReadResponseAsync(userId.Value);
    }

    public async Task<ApiResponse<List<NotificationResponse>>> DeleteManyAsync(string patientId, List<string> notificationIds)
    {
        var userId = await UserIdAsync(patientId);
        if (userId == null) return ApiResponse<List<NotificationResponse>>.Fail("This account is not a patient account.", 403);

        var ids = CleanIds(notificationIds);
        if (ids.Count > 0)
        {
            var rows = await _db.Notifications.Where(item => item.UserId == userId && ids.Contains(item.NotificationId) && item.Status != "Deleted").ToListAsync();
            foreach (var row in rows) row.Status = "Deleted";
            await _db.SaveChangesAsync();
        }

        return await ReadResponseAsync(userId.Value);
    }

    private async Task SynchronizeAsync(string patientId, long userId)
    {
        var now = DateTimeHelper.MalaysiaNow();
        var drafts = new Dictionary<string, NotificationDraft>(StringComparer.Ordinal);

        var appointments = await _db.Appointments
            .AsNoTracking()
            .Include(row => row.Doctor).ThenInclude(row => row.User)
            .Where(row => row.PatientId == patientId && row.Status == "Scheduled")
            .ToListAsync();
        foreach (var appointment in appointments)
        {
            var start = appointment.AppointmentDate.Date + appointment.AppointmentTime;
            var doctor = appointment.Doctor?.User?.Name ?? "your doctor";
            if (start > now && start <= now.AddDays(7))
            {
                var when = FormatDateTime(start);
                Add(drafts, new NotificationDraft(
                    $"appointment-reminder:{appointment.AppointmentId}",
                    "Appointment reminder",
                    $"Your appointment with {doctor} is scheduled for {when}.",
                    "预约提醒",
                    $"您与{doctor}的预约时间为{FormatDateTimeZh(start)}。",
                    doctor,
                    Initials(doctor),
                    "appointment_reminder",
                    "/visits",
                    now));
            }

            var checkInOpens = start.AddMinutes(-10);
            var checkInCloses = start.AddMinutes(QueueServices.QueueServices.CheckInMinutesAfter);
            if (appointment.CheckInStatus.Equals("Pending", StringComparison.OrdinalIgnoreCase)
                && now >= checkInOpens
                && now <= checkInCloses)
            {
                Add(drafts, new NotificationDraft(
                    $"check-in-reminder:{appointment.AppointmentId}",
                    "Check in for your appointment",
                    $"Your appointment with {doctor} starts at {start:h:mm tt}. Check in by {checkInCloses:h:mm tt} to receive your queue number and avoid automatic cancellation.",
                    "预约报到提醒",
                    $"您与{doctor}的预约将于{start:HH:mm}开始。请在{checkInCloses:HH:mm}前报到，以获取排队号码并避免预约自动取消。",
                    doctor,
                    Initials(doctor),
                    "check_in_reminder",
                    $"/queue?appointmentId={Uri.EscapeDataString(appointment.AppointmentId)}",
                    checkInOpens));
            }
        }

        var queueEntries = await _db.QueueEntries
            .AsNoTracking()
            .Include(row => row.Appointment).ThenInclude(row => row!.Doctor).ThenInclude(row => row.User)
            .Include(row => row.Room)
            .Where(row => row.PatientId == patientId
                && (row.Status == "Waiting" || row.Status == "Called" || row.Status == "InConsultation"))
            .ToListAsync();
        foreach (var entry in queueEntries)
        {
            var group = await _db.QueueEntries.AsNoTracking()
                .Where(row => row.QueueType == entry.QueueType)
                .OrderBy(row => row.CheckInTime)
                .ThenBy(row => row.QueueId)
                .ToListAsync();
            var position = Math.Max(1, group.FindIndex(row => row.QueueId == entry.QueueId) + 1);
            var peopleAhead = entry.Status.Equals("Waiting", StringComparison.OrdinalIgnoreCase)
                ? group.Count(row => row.QueueId != entry.QueueId
                    && row.CheckInTime <= entry.CheckInTime
                    && (row.Status == "Waiting" || row.Status == "Called" || row.Status == "InConsultation"))
                : 0;
            var queueNumber = $"Q{position:000}";
            var doctor = entry.Appointment?.Doctor?.User?.Name ?? "CarePlus doctor";
            var message = entry.Status switch
            {
                "Called" => entry.Room == null
                    ? $"Queue {queueNumber} has been called. Please proceed to the clinic counter."
                    : $"Queue {queueNumber} has been called. Please proceed to {entry.Room.RoomName}.",
                "InConsultation" => $"Queue {queueNumber} is now in consultation.",
                _ => $"You are {queueNumber}. {peopleAhead} patient(s) are ahead of you.",
            };
            var messageZh = entry.Status switch
            {
                "Called" => entry.Room == null
                    ? $"排队号码 {queueNumber} 已被叫号。请前往诊所柜台。"
                    : $"排队号码 {queueNumber} 已被叫号。请前往{entry.Room.RoomName}。",
                "InConsultation" => $"排队号码 {queueNumber} 正在看诊。",
                _ => $"您的排队号码是 {queueNumber}，前面还有{peopleAhead}位病人。",
            };
            Add(drafts, new NotificationDraft(
                $"queue-update:{entry.QueueId}:{entry.Status.ToLowerInvariant()}",
                "Queue update",
                message,
                "排队更新",
                messageZh,
                doctor,
                Initials(doctor),
                "queue_update",
                "/queue",
                entry.CheckInTime));
        }

        var invoices = await _db.Invoices
            .AsNoTracking()
            .Where(row => row.PatientId == patientId && row.PaymentStatus != "Paid")
            .ToListAsync();
        foreach (var invoice in invoices)
        {
            var dueDate = invoice.DueDate?.Date;
            if (dueDate == null) continue;

            var overdueDays = Math.Max(0, (now.Date - dueDate.Value).Days);
            var amount = invoice.TotalAmount + overdueDays * Payment.LateFeePerDay;
            var message = overdueDays > 0
                ? $"Invoice {invoice.InvoiceId} is overdue by {overdueDays} day(s). Your current amount due is RM {amount:0.00}."
                : $"Invoice {invoice.InvoiceId} is due on {dueDate:dd MMM yyyy}. Amount due: RM {amount:0.00}.";
            var messageZh = overdueDays > 0
                ? $"账单 {invoice.InvoiceId} 已逾期{overdueDays}天，目前应付金额为 RM {amount:0.00}。"
                : $"账单 {invoice.InvoiceId} 将于{dueDate:yyyy年MM月dd日}到期，应付金额为 RM {amount:0.00}。";
            Add(drafts, new NotificationDraft(
                $"payment-reminder:{invoice.InvoiceId}",
                "Payment reminder",
                message,
                "付款提醒",
                messageZh,
                "CarePlus Billing",
                "CB",
                "payment_reminder",
                "/invoices",
                now));
        }

        var orders = await _db.MedicineOrders
            .AsNoTracking()
            .Where(row => row.PatientId == patientId && row.Status != "delivered" && row.Status != "picked_up" && row.Status != "cancelled")
            .ToListAsync();
        foreach (var order in orders)
        {
            var status = order.Status.Trim().ToLowerInvariant();
            var statusText = status == "ready_pickup" ? "is ready for pickup" : status == "processing" ? "is being prepared" : "has a delivery update";
            var statusTextZh = status == "ready_pickup" ? "已准备好领取" : status == "processing" ? "正在准备中" : "有新的配送更新";
            Add(drafts, new NotificationDraft(
                $"medicine-update:{order.OrderId}",
                "Medicine order update",
                $"Medicine order {order.OrderId} {statusText}.",
                "药品订单更新",
                $"药品订单 {order.OrderId}{statusTextZh}。",
                "CarePlus Pharmacy",
                "CP",
                "medicine_update",
                "/orders",
                order.OrderDate));
        }

        var invitations = await _db.FamilyMembers
            .AsNoTracking()
            .Include(row => row.FamilyGroup).ThenInclude(row => row.Patient).ThenInclude(row => row.User)
            .Where(row => row.PatientId == patientId && row.Status == "Pending")
            .ToListAsync();
        foreach (var invitation in invitations)
        {
            var owner = invitation.FamilyGroup?.Patient?.User?.Name ?? "your family owner";
            var familyName = invitation.FamilyGroup?.GroupName ?? "your family";
            Add(drafts, new NotificationDraft(
                $"family-invite:{invitation.FamilyMemberId}",
                "Family invitation",
                $"{owner} invited you to join {familyName}.",
                "家庭邀请",
                $"{owner}邀请您加入{familyName}。",
                owner,
                Initials(owner),
                "family_invitation",
                "/family",
                invitation.JoinAt));
        }

        var announcements = await _db.Notifications
            .AsNoTracking()
            .Where(row => row.UserId == null
                && row.Type == "announcement"
                && row.IsPublished
                && row.Status != "Deleted"
                && row.CreatedAt <= now)
            .ToListAsync();
        var announcementStartAt = await _db.Patients.AsNoTracking()
            .Where(row => row.PatientId == patientId)
            .Select(row => row.AnnouncementStartAt)
            .FirstOrDefaultAsync();
        foreach (var announcement in announcements)
        {
            if (announcementStartAt.HasValue && announcement.CreatedAt < announcementStartAt.Value) continue;
            Add(drafts, new NotificationDraft(
                $"announcement:{announcement.NotificationId}:user:{userId}",
                announcement.Title,
                announcement.Message,
                announcement.TitleZh ?? announcement.Title,
                announcement.MessageZh ?? announcement.Message,
                announcement.Actor,
                announcement.Initials,
                "announcement",
                $"/announcements#{announcement.NotificationId}",
                announcement.CreatedAt));
        }

        var ids = drafts.Keys.ToList();
        var existing = await _db.Notifications.Where(row => row.UserId == userId).ToListAsync();
        var byId = existing.ToDictionary(row => row.NotificationId, StringComparer.Ordinal);
        foreach (var draft in drafts.Values)
        {
            if (!byId.TryGetValue(draft.Id, out var row))
            {
                row = new Notification
                {
                    NotificationId = draft.Id,
                    UserId = userId,
                    CreatedAt = draft.CreatedAt,
                    Status = "Unread",
                };
                _db.Notifications.Add(row);
                byId[draft.Id] = row;
            }

            row.Title = draft.Title;
            row.Message = draft.Message;
            row.TitleZh = draft.TitleZh;
            row.MessageZh = draft.MessageZh;
            row.Actor = draft.Actor;
            row.Initials = draft.Initials;
            row.Type = draft.Type;
            row.Href = draft.Href;
        }

        var preference = await _db.AppSettings.AsNoTracking().Where(row => row.UserId == userId).Select(row => row.Notification).FirstOrDefaultAsync() ?? "all";
        foreach (var row in byId.Values)
        {
            if (row.Status == "Deleted") continue;
            if (preference.Equals("none", StringComparison.OrdinalIgnoreCase)
                || (preference.Equals("reminder", StringComparison.OrdinalIgnoreCase) && !IsReminder(row.Type)))
            {
                row.Status = "Read";
            }
        }

        var currentIds = drafts.Keys.ToHashSet(StringComparer.Ordinal);
        foreach (var row in existing.Where(row => row.Status == "Unread" && IsGenerated(row.NotificationId) && !currentIds.Contains(row.NotificationId)))
        {
            row.Status = "Read";
        }

        await _db.SaveChangesAsync();
    }

    private async Task<ApiResponse<List<NotificationResponse>>> ReadResponseAsync(long userId)
    {
        var rows = await _db.Notifications.AsNoTracking()
            .Where(row => row.UserId == userId && row.Status != "Deleted")
            .OrderByDescending(row => row.CreatedAt)
            .ToListAsync();
        var response = rows.Select(row => new NotificationResponse
        {
            Id = row.NotificationId,
            Title = row.Title,
            Message = row.Message,
            TitleZh = row.TitleZh ?? row.Title,
            MessageZh = row.MessageZh ?? row.Message,
            Actor = row.Actor,
            Initials = row.Initials,
            Type = row.Type,
            Href = row.Href,
            CreatedAt = ToIso(row.CreatedAt),
            Unread = row.Status.Equals("Unread", StringComparison.OrdinalIgnoreCase),
        }).ToList();
        return ApiResponse<List<NotificationResponse>>.Ok(response);
    }

    private async Task<long?> UserIdAsync(string patientId) => await _db.Patients.Where(row => row.PatientId == patientId).Select(row => (long?)row.UserId).FirstOrDefaultAsync();

    private static void Add(Dictionary<string, NotificationDraft> drafts, NotificationDraft draft) => drafts[draft.Id] = draft;

    private static bool IsReminder(string type) => type is "appointment_reminder" or "appointment_cancelled" or "check_in_reminder" or "queue_update" or "payment_reminder" or "medicine_update";

    private static bool IsGenerated(string id) => id.StartsWith("appointment-reminder:", StringComparison.Ordinal)
        || id.StartsWith("check-in-reminder:", StringComparison.Ordinal)
        || id.StartsWith("queue-update:", StringComparison.Ordinal)
        || id.StartsWith("payment-reminder:", StringComparison.Ordinal)
        || id.StartsWith("medicine-update:", StringComparison.Ordinal)
        || id.StartsWith("family-invite:", StringComparison.Ordinal)
        || id.StartsWith("announcement:", StringComparison.Ordinal);

    private static List<string> CleanIds(IEnumerable<string>? ids) => (ids ?? []).Where(id => !string.IsNullOrWhiteSpace(id)).Select(id => id.Trim()).Distinct(StringComparer.Ordinal).Take(100).ToList();

    private static string Initials(string value)
    {
        var parts = value.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return string.Concat(parts.Take(2).Select(part => char.ToUpperInvariant(part[0])));
    }

    private static string FormatDateTime(DateTime value) => value.ToString("dd MMM yyyy, h:mm tt", CultureInfo.InvariantCulture);
    private static string FormatDateTimeZh(DateTime value) => value.ToString("yyyy年MM月dd日 HH:mm", CultureInfo.InvariantCulture);
    private static string ToIso(DateTime value) => new DateTimeOffset(DateTime.SpecifyKind(value, DateTimeKind.Unspecified), TimeSpan.FromHours(8)).ToString("yyyy-MM-ddTHH:mm:sszzz", CultureInfo.InvariantCulture);

    private sealed record NotificationDraft(
        string Id,
        string Title,
        string Message,
        string TitleZh,
        string MessageZh,
        string Actor,
        string Initials,
        string Type,
        string Href,
        DateTime CreatedAt);
}
