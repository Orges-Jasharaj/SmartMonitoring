using MonitoringService.Constants;
using MonitoringService.Data;
using MonitoringService.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace MonitoringService.Services;

public interface IAlertEvaluator
{
    Task EvaluateReadingAsync(Device device, decimal temperatureC, CancellationToken cancellationToken = default);
}

public class AlertEvaluator(MonitoringDbContext dbContext) : IAlertEvaluator
{
    public async Task EvaluateReadingAsync(Device device, decimal temperatureC, CancellationToken cancellationToken = default)
    {
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
                return;
            }

            dbContext.Alerts.Add(new Alert
            {
                Id = Guid.NewGuid(),
                DeviceId = device.Id,
                CompanyId = device.CompanyId,
                AlertType = AlertTypes.TemperatureOutOfRange,
                Message = $"Temperature {temperatureC}°C is outside allowed range {device.MinTempC}°C to {device.MaxTempC}°C for device '{device.Name}'.",
                TemperatureC = temperatureC,
                TriggeredAtUtc = DateTime.UtcNow,
                IsActive = true
            });
        }
        else if (activeAlert != null)
        {
            activeAlert.IsActive = false;
            activeAlert.ResolvedAtUtc = DateTime.UtcNow;

            dbContext.Alerts.Add(new Alert
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
            });
        }
    }
}
