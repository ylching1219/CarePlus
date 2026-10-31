using CarePlus.API.Data;
using CarePlus.API.DTO;
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
            if (start <= now || start > now.AddDays(7)) continue;

            var doctor = appointment.Doctor?.User?.Name ?? "your doctor";
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
        foreach (var announcement in announcements)
        {
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

    private static bool IsReminder(string type) => type is "appointment_reminder" or "payment_reminder" or "medicine_update";

    private static bool IsGenerated(string id) => id.StartsWith("appointment-reminder:", StringComparison.Ordinal)
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
