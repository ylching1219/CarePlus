using CarePlus.API.Data;
using CarePlus.API.Extension;
using CarePlus.API.Models.Notification;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Globalization;

namespace CarePlus.API.Services.QueueServices;

/// <summary>
/// Cancels scheduled appointments whose 30-minute check-in window has expired.
/// This runs on the API server so the rule is enforced even when the patient is offline.
/// </summary>
public sealed class AppointmentCheckInMonitor : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(30);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<AppointmentCheckInMonitor> _logger;

    public AppointmentCheckInMonitor(
        IServiceScopeFactory scopeFactory,
        ILogger<AppointmentCheckInMonitor> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CancelExpiredAppointmentsAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Unable to process expired appointment check-in windows.");
            }

            await Task.Delay(PollInterval, stoppingToken);
        }
    }

    private async Task CancelExpiredAppointmentsAsync(CancellationToken cancellationToken)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        var now = DateTimeHelper.MalaysiaNow();

        var candidates = await db.Appointments
            .Include(row => row.Patient)
                .ThenInclude(row => row.User)
            .Include(row => row.Doctor)
                .ThenInclude(row => row.User)
            .Where(row => row.Status == "Scheduled" && row.CheckInStatus == "Pending")
            .ToListAsync(cancellationToken);

        var expired = candidates
            .Where(row => StartAt(row).AddMinutes(QueueServices.CheckInMinutesAfter) < now)
            .ToList();
        if (expired.Count == 0)
        {
            await transaction.CommitAsync(cancellationToken);
            return;
        }

        var appointmentIds = expired.Select(row => row.AppointmentId).ToList();
        var checkedInIds = await db.QueueEntries
            .AsNoTracking()
            .Where(row => row.AppointmentId != null && appointmentIds.Contains(row.AppointmentId))
            .Select(row => row.AppointmentId!)
            .ToHashSetAsync(cancellationToken);

        var userIds = expired
            .Where(row => !checkedInIds.Contains(row.AppointmentId))
            .Select(row => row.Patient.UserId)
            .Distinct()
            .ToList();
        var preferences = await db.AppSettings
            .AsNoTracking()
            .Where(row => userIds.Contains(row.UserId))
            .ToDictionaryAsync(row => row.UserId, row => row.Notification, cancellationToken);

        var cancelledCount = 0;
        foreach (var appointment in expired)
        {
            if (checkedInIds.Contains(appointment.AppointmentId))
            {
                continue;
            }

            appointment.Status = "Cancelled";
            appointment.CheckInStatus = "Missed";
            appointment.CancelledAt = now;

            var doctor = DoctorName(appointment.Doctor?.User?.Name);
            var start = StartAt(appointment);
            var notificationId = $"check-in-cancelled:{appointment.AppointmentId}";
            var notification = await db.Notifications
                .FirstOrDefaultAsync(row => row.NotificationId == notificationId, cancellationToken);
            if (notification == null)
            {
                notification = new Notification
                {
                    NotificationId = notificationId,
                    UserId = appointment.Patient.UserId,
                    Status = preferences.TryGetValue(appointment.Patient.UserId, out var preference)
                        && preference.Equals("none", StringComparison.OrdinalIgnoreCase)
                            ? "Read"
                            : "Unread",
                    CreatedAt = now,
                };
                db.Notifications.Add(notification);
            }

            notification.Title = "Appointment cancelled";
            notification.Message = $"Your appointment with {doctor} on {FormatDateTime(start)} was cancelled because you did not check in during the 30-minute check-in window.";
            notification.TitleZh = "预约已取消";
            notification.MessageZh = $"您与{doctor}在{FormatDateTimeZh(start)}的预约已取消，因为您未在 30 分钟报到时段内完成报到。";
            notification.Actor = doctor;
            notification.Initials = Initials(doctor);
            notification.Type = "appointment_cancelled";
            notification.Href = "/visits";
            notification.IsPublished = true;
            cancelledCount++;
        }

        if (cancelledCount == 0)
        {
            await transaction.CommitAsync(cancellationToken);
            return;
        }

        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        _logger.LogInformation("Cancelled {Count} appointment(s) after missed check-in windows.", cancelledCount);
    }

    private static DateTime StartAt(Models.Appointment.Appointment appointment) =>
        appointment.AppointmentDate.Date + appointment.AppointmentTime;

    private static string DoctorName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name)) return "your CarePlus doctor";
        var trimmed = name.Trim();
        return trimmed.StartsWith("Dr ", StringComparison.OrdinalIgnoreCase) ? trimmed : $"Dr {trimmed}";
    }

    private static string Initials(string value)
    {
        var parts = value.Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Where(part => !part.Equals("Dr", StringComparison.OrdinalIgnoreCase));
        var initials = string.Concat(parts.Take(2).Select(part => char.ToUpperInvariant(part[0])));
        return string.IsNullOrWhiteSpace(initials) ? "CP" : initials;
    }

    private static string FormatDateTime(DateTime value) =>
        value.ToString("dd MMM yyyy, h:mm tt", CultureInfo.InvariantCulture);

    private static string FormatDateTimeZh(DateTime value) =>
        value.ToString("yyyy年MM月dd日 HH:mm", CultureInfo.InvariantCulture);
}
