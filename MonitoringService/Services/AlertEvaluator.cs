using MonitoringService.Constants;
using MonitoringService.Data;
using MonitoringService.Data.Models;
using MonitoringService.Features.Readings.Commands;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace MonitoringService.Services;

public interface IAlertEvaluator
{
    Task<IReadOnlyList<Alert>> EvaluateReadingAsync(
        Device device,
        IngestReadingCommand reading,
        CancellationToken cancellationToken = default);
}

public class AlertEvaluator(
    MonitoringDbContext dbContext,
    IOptions<AlertOptions> alertOptions) : IAlertEvaluator
{
    public async Task<IReadOnlyList<Alert>> EvaluateReadingAsync(
        Device device,
        IngestReadingCommand reading,
        CancellationToken cancellationToken = default)
    {
        var alertsToNotify = new List<Alert>();

        await EvaluateRangeAsync(
            device,
            reading.TemperatureC,
            device.MinTempC,
            device.MaxTempC,
            AlertTypes.TemperatureOutOfRange,
            AlertTypes.TemperatureNormalized,
            "Temperature",
            "°C",
            alertsToNotify,
            cancellationToken);

        if (reading.HumidityPct.HasValue)
        {
            await EvaluateRangeAsync(
                device,
                reading.HumidityPct.Value,
                device.MinHumidityPct,
                device.MaxHumidityPct,
                AlertTypes.HumidityOutOfRange,
                AlertTypes.HumidityNormalized,
                "Humidity",
                "%",
                alertsToNotify,
                cancellationToken);
        }

        if (reading.Co2Ppm.HasValue)
        {
            await EvaluateRangeAsync(
                device,
                reading.Co2Ppm.Value,
                device.MinCo2Ppm,
                device.MaxCo2Ppm,
                AlertTypes.Co2OutOfRange,
                AlertTypes.Co2Normalized,
                "CO₂",
                " ppm",
                alertsToNotify,
                cancellationToken);
        }

        if (reading.LightLevelLux.HasValue)
        {
            await EvaluateRangeAsync(
                device,
                reading.LightLevelLux.Value,
                device.MinLightLevelLux,
                device.MaxLightLevelLux,
                AlertTypes.LightLevelOutOfRange,
                AlertTypes.LightLevelNormalized,
                "Light level",
                " lux",
                alertsToNotify,
                cancellationToken);
        }

        if (reading.NoiseLevelDb.HasValue)
        {
            await EvaluateRangeAsync(
                device,
                reading.NoiseLevelDb.Value,
                device.MinNoiseLevelDb,
                device.MaxNoiseLevelDb,
                AlertTypes.NoiseLevelOutOfRange,
                AlertTypes.NoiseLevelNormalized,
                "Noise level",
                " dB",
                alertsToNotify,
                cancellationToken);
        }

        if (reading.BatteryLevelPct.HasValue)
        {
            await EvaluateLowThresholdAsync(
                device,
                reading.BatteryLevelPct.Value,
                device.MinBatteryPct,
                AlertTypes.LowBattery,
                AlertTypes.BatteryNormalized,
                "Battery",
                "%",
                alertsToNotify,
                cancellationToken);
        }

        return alertsToNotify;
    }

    private async Task EvaluateRangeAsync(
        Device device,
        decimal value,
        decimal min,
        decimal max,
        string outOfRangeType,
        string normalizedType,
        string metricName,
        string unit,
        List<Alert> alertsToNotify,
        CancellationToken cancellationToken)
    {
        var isOutOfRange = value < min || value > max;
        var activeAlert = await FindActiveAlertAsync(device.Id, outOfRangeType, cancellationToken);

        if (isOutOfRange)
        {
            if (activeAlert != null)
            {
                activeAlert.TemperatureC = value;
                activeAlert.Message = BuildRangeMessage(device, metricName, value, unit, min, max, isStillOutOfRange: true);
                if (ShouldSendReminder(activeAlert))
                {
                    alertsToNotify.Add(activeAlert);
                }

                return;
            }

            var alert = CreateAlert(
                device,
                outOfRangeType,
                BuildRangeMessage(device, metricName, value, unit, min, max, isStillOutOfRange: false),
                value);
            dbContext.Alerts.Add(alert);
            alertsToNotify.Add(alert);
            return;
        }

        if (activeAlert != null)
        {
            await ResolveAlertAsync(device, activeAlert, normalizedType, metricName, value, unit, alertsToNotify, cancellationToken);
        }
    }

    private async Task EvaluateHighThresholdAsync(
        Device device,
        decimal value,
        decimal max,
        string outOfRangeType,
        string normalizedType,
        string metricName,
        string unit,
        List<Alert> alertsToNotify,
        CancellationToken cancellationToken)
    {
        var isHigh = value > max;
        var activeAlert = await FindActiveAlertAsync(device.Id, outOfRangeType, cancellationToken);

        if (isHigh)
        {
            if (activeAlert != null)
            {
                activeAlert.TemperatureC = value;
                activeAlert.Message = BuildHighMessage(device, metricName, value, unit, max, isStillHigh: true);
                if (ShouldSendReminder(activeAlert))
                {
                    alertsToNotify.Add(activeAlert);
                }

                return;
            }

            var alert = CreateAlert(
                device,
                outOfRangeType,
                BuildHighMessage(device, metricName, value, unit, max, isStillHigh: false),
                value);
            dbContext.Alerts.Add(alert);
            alertsToNotify.Add(alert);
            return;
        }

        if (activeAlert != null)
        {
            await ResolveAlertAsync(device, activeAlert, normalizedType, metricName, value, unit, alertsToNotify, cancellationToken);
        }
    }

    private async Task EvaluateLowThresholdAsync(
        Device device,
        decimal value,
        decimal min,
        string outOfRangeType,
        string normalizedType,
        string metricName,
        string unit,
        List<Alert> alertsToNotify,
        CancellationToken cancellationToken)
    {
        var isLow = value < min;
        var activeAlert = await FindActiveAlertAsync(device.Id, outOfRangeType, cancellationToken);

        if (isLow)
        {
            if (activeAlert != null)
            {
                activeAlert.TemperatureC = value;
                activeAlert.Message = BuildLowMessage(device, metricName, value, unit, min, isStillLow: true);
                if (ShouldSendReminder(activeAlert))
                {
                    alertsToNotify.Add(activeAlert);
                }

                return;
            }

            var alert = CreateAlert(
                device,
                outOfRangeType,
                BuildLowMessage(device, metricName, value, unit, min, isStillLow: false),
                value);
            dbContext.Alerts.Add(alert);
            alertsToNotify.Add(alert);
            return;
        }

        if (activeAlert != null)
        {
            await ResolveAlertAsync(device, activeAlert, normalizedType, metricName, value, unit, alertsToNotify, cancellationToken);
        }
    }

    private Task<Alert?> FindActiveAlertAsync(Guid deviceId, string alertType, CancellationToken cancellationToken)
    {
        return dbContext.Alerts.FirstOrDefaultAsync(
            a => a.DeviceId == deviceId && a.IsActive && a.AlertType == alertType,
            cancellationToken);
    }

    private async Task ResolveAlertAsync(
        Device device,
        Alert activeAlert,
        string normalizedType,
        string metricName,
        decimal value,
        string unit,
        List<Alert> alertsToNotify,
        CancellationToken cancellationToken)
    {
        activeAlert.IsActive = false;
        activeAlert.ResolvedAtUtc = DateTime.UtcNow;

        var resolvedAlert = CreateAlert(
            device,
            normalizedType,
            $"{metricName} {value}{unit} returned to normal for device '{device.Name}'.",
            value,
            isActive: false,
            resolvedAtUtc: DateTime.UtcNow);
        dbContext.Alerts.Add(resolvedAlert);
        alertsToNotify.Add(resolvedAlert);
        await Task.CompletedTask;
    }

    private static Alert CreateAlert(
        Device device,
        string alertType,
        string message,
        decimal value,
        bool isActive = true,
        DateTime? resolvedAtUtc = null)
    {
        return new Alert
        {
            Id = Guid.NewGuid(),
            DeviceId = device.Id,
            CompanyId = device.CompanyId,
            AlertType = alertType,
            Message = message,
            TemperatureC = value,
            TriggeredAtUtc = DateTime.UtcNow,
            IsActive = isActive,
            ResolvedAtUtc = resolvedAtUtc
        };
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

    private static string BuildRangeMessage(
        Device device,
        string metricName,
        decimal value,
        string unit,
        decimal min,
        decimal max,
        bool isStillOutOfRange)
    {
        var state = isStillOutOfRange ? "is still outside" : "is outside";
        return $"{metricName} {value}{unit} {state} allowed range {min}{unit} to {max}{unit} for device '{device.Name}'.";
    }

    private static string BuildHighMessage(
        Device device,
        string metricName,
        decimal value,
        string unit,
        decimal max,
        bool isStillHigh)
    {
        var state = isStillHigh ? "is still above" : "is above";
        return $"{metricName} {value}{unit} {state} limit {max}{unit} for device '{device.Name}'.";
    }

    private static string BuildLowMessage(
        Device device,
        string metricName,
        decimal value,
        string unit,
        decimal min,
        bool isStillLow)
    {
        var state = isStillLow ? "is still below" : "is below";
        return $"{metricName} {value}{unit} {state} minimum {min}{unit} for device '{device.Name}'.";
    }
}
