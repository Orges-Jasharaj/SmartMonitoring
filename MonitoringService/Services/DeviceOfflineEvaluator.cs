using MonitoringService.Constants;
using MonitoringService.Data;
using MonitoringService.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace MonitoringService.Services;

public interface IDeviceOfflineEvaluator
{
    Task<IReadOnlyList<Alert>> EvaluateOfflineDevicesAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Alert>> EvaluateDeviceReadingAsync(Device device, CancellationToken cancellationToken = default);
}

public class DeviceOfflineEvaluator(
    MonitoringDbContext dbContext,
    IOptions<AlertOptions> alertOptions) : IDeviceOfflineEvaluator
{
    public async Task<IReadOnlyList<Alert>> EvaluateOfflineDevicesAsync(CancellationToken cancellationToken = default)
    {
        var alertsToNotify = new List<Alert>();
        var offlineAfter = TimeSpan.FromMinutes(alertOptions.Value.DeviceOfflineAfterMinutes);
        var cutoff = DateTime.UtcNow - offlineAfter;

        var offlineDevices = await dbContext.Devices
            .Where(d => d.IsActive && d.LastReadingAtUtc != null && d.LastReadingAtUtc < cutoff)
            .ToListAsync(cancellationToken);

        foreach (var device in offlineDevices)
        {
            var activeAlert = await dbContext.Alerts
                .FirstOrDefaultAsync(
                    a => a.DeviceId == device.Id
                         && a.IsActive
                         && a.AlertType == AlertTypes.DeviceOffline,
                    cancellationToken);

            if (activeAlert != null)
            {
                activeAlert.Message = BuildOfflineMessage(device, device.LastReadingAtUtc!.Value);

                if (ShouldSendReminder(activeAlert))
                {
                    alertsToNotify.Add(activeAlert);
                }

                continue;
            }

            var alert = new Alert
            {
                Id = Guid.NewGuid(),
                DeviceId = device.Id,
                CompanyId = device.CompanyId,
                AlertType = AlertTypes.DeviceOffline,
                Message = BuildOfflineMessage(device, device.LastReadingAtUtc!.Value),
                TriggeredAtUtc = DateTime.UtcNow,
                IsActive = true
            };
            dbContext.Alerts.Add(alert);
            alertsToNotify.Add(alert);
        }

        return alertsToNotify;
    }

    public async Task<IReadOnlyList<Alert>> EvaluateDeviceReadingAsync(Device device, CancellationToken cancellationToken = default)
    {
        var activeAlert = await dbContext.Alerts
            .FirstOrDefaultAsync(
                a => a.DeviceId == device.Id
                     && a.IsActive
                     && a.AlertType == AlertTypes.DeviceOffline,
                cancellationToken);

        if (activeAlert == null)
        {
            return [];
        }

        activeAlert.IsActive = false;
        activeAlert.ResolvedAtUtc = DateTime.UtcNow;
        return [activeAlert];
    }

    private bool ShouldSendReminder(Alert activeAlert)
    {
        var reminderMinutes = alertOptions.Value.ActiveAlertReminderMinutes;
        if (reminderMinutes <= 0)
        {
            return true;
        }

        if (activeAlert.LastNotifiedAtUtc == null)
        {
            return true;
        }

        return DateTime.UtcNow - activeAlert.LastNotifiedAtUtc.Value >= TimeSpan.FromMinutes(reminderMinutes);
    }

    private static string BuildOfflineMessage(Device device, DateTime lastReadingAtUtc)
    {
        var minutesAgo = Math.Max(1, (int)Math.Round((DateTime.UtcNow - lastReadingAtUtc).TotalMinutes));
        return $"Device '{device.Name}' has not reported a temperature reading for {minutesAgo} minutes (last reading at {lastReadingAtUtc:u}).";
    }
}
