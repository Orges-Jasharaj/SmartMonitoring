using MonitoringService.Constants;
using MonitoringService.Data;
using MonitoringService.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace MonitoringService.Services;

public interface IAlertEvaluator
{
    Task<IReadOnlyList<Alert>> EvaluateReadingAsync(Device device, decimal temperatureC, CancellationToken cancellationToken = default);
}

public class AlertEvaluator(
    MonitoringDbContext dbContext,
    IOptions<AlertOptions> alertOptions) : IAlertEvaluator
{
    public async Task<IReadOnlyList<Alert>> EvaluateReadingAsync(Device device, decimal temperatureC, CancellationToken cancellationToken = default)
    {
        var alertsToNotify = new List<Alert>();
        var isOutOfRange = temperatureC < device.MinTempC || temperatureC > device.MaxTempC;

        var activeAlert = await dbContext.Alerts
            .FirstOrDefaultAsync(
                a => a.DeviceId == device.Id
                     && a.IsActive
                     && a.AlertType == AlertTypes.TemperatureOutOfRange,
                cancellationToken);

        if (isOutOfRange)
        {
            if (activeAlert != null)
            {
                UpdateActiveOutOfRangeAlert(activeAlert, device, temperatureC);

                if (ShouldSendReminder(activeAlert))
                {
                    alertsToNotify.Add(activeAlert);
                }

                return alertsToNotify;
            }

            var alert = new Alert
            {
                Id = Guid.NewGuid(),
                DeviceId = device.Id,
                CompanyId = device.CompanyId,
                AlertType = AlertTypes.TemperatureOutOfRange,
                Message = BuildOutOfRangeMessage(device, temperatureC, isStillOutOfRange: false),
                TemperatureC = temperatureC,
                TriggeredAtUtc = DateTime.UtcNow,
                IsActive = true
            };
            dbContext.Alerts.Add(alert);
            alertsToNotify.Add(alert);
        }
        else if (activeAlert != null)
        {
            activeAlert.IsActive = false;
            activeAlert.ResolvedAtUtc = DateTime.UtcNow;

            var resolvedAlert = new Alert
            {
                Id = Guid.NewGuid(),
                DeviceId = device.Id,
                CompanyId = device.CompanyId,
                AlertType = AlertTypes.TemperatureNormalized,
                Message = $"Temperature {temperatureC}°C returned to normal for device '{device.Name}'.",
                TemperatureC = temperatureC,
                TriggeredAtUtc = DateTime.UtcNow,
                IsActive = false,
                ResolvedAtUtc = DateTime.UtcNow
            };
            dbContext.Alerts.Add(resolvedAlert);
            alertsToNotify.Add(resolvedAlert);
        }

        return alertsToNotify;
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

    private static void UpdateActiveOutOfRangeAlert(Alert activeAlert, Device device, decimal temperatureC)
    {
        activeAlert.TemperatureC = temperatureC;
        activeAlert.Message = BuildOutOfRangeMessage(device, temperatureC, isStillOutOfRange: true);
    }

    private static string BuildOutOfRangeMessage(Device device, decimal temperatureC, bool isStillOutOfRange)
    {
        var state = isStillOutOfRange ? "is still outside" : "is outside";
        return $"Temperature {temperatureC}°C {state} allowed range {device.MinTempC}°C to {device.MaxTempC}°C for device '{device.Name}'.";
    }
}
