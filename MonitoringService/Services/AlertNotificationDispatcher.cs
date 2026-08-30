using MonitoringService.Constants;
using MonitoringService.Data;
using MonitoringService.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SmartMonitoring.Shared.Notifications;

namespace MonitoringService.Services;

public interface IAlertNotificationDispatcher
{
    Task DispatchAsync(Device device, IReadOnlyList<Alert> alerts, CancellationToken cancellationToken = default);
}

public class AlertNotificationDispatcher(
    MonitoringDbContext dbContext,
    IIdentityUserEmailResolver emailResolver,
    INotificationPublisher notificationPublisher,
    IOptions<NotificationOptions> notificationOptions,
    ILogger<AlertNotificationDispatcher> logger) : IAlertNotificationDispatcher
{
    public async Task DispatchAsync(Device device, IReadOnlyList<Alert> alerts, CancellationToken cancellationToken = default)
    {
        if (!notificationOptions.Value.Enabled || alerts.Count == 0)
        {
            return;
        }

        var userIds = await dbContext.CompanyUsers
            .Where(m => m.CompanyId == device.CompanyId)
            .Select(m => m.UserId)
            .ToListAsync(cancellationToken);

        var recipientEmails = await emailResolver.ResolveEmailsAsync(userIds, cancellationToken);
        if (recipientEmails.Count == 0)
        {
            logger.LogWarning(
                "No recipient emails found for company {CompanyId}. Assign users to the company to receive alert emails.",
                device.CompanyId);
            return;
        }

        var anyNotified = false;

        foreach (var alert in alerts)
        {
            var request = new AlertNotificationRequest
            {
                AlertId = alert.Id,
                CompanyId = alert.CompanyId,
                DeviceId = alert.DeviceId,
                DeviceName = device.Name,
                ZoneName = device.ZoneName,
                AlertType = alert.AlertType,
                Message = alert.Message,
                TemperatureC = alert.TemperatureC,
                TriggeredAtUtc = alert.TriggeredAtUtc,
                RecipientEmails = recipientEmails.ToList()
            };

            try
            {
                await notificationPublisher.PublishAlertAsync(request, cancellationToken);

                if (alert.AlertType is AlertTypes.TemperatureOutOfRange or AlertTypes.DeviceOffline)
                {
                    alert.LastNotifiedAtUtc = DateTime.UtcNow;
                    anyNotified = true;
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to publish notification for alert {AlertId}", alert.Id);
            }
        }

        if (anyNotified)
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
