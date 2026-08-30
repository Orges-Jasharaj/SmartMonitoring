using MonitoringService.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace MonitoringService.Services;

public class DeviceOfflineMonitorService(
    IServiceScopeFactory scopeFactory,
    IOptions<AlertOptions> alertOptions,
    ILogger<DeviceOfflineMonitorService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var interval = TimeSpan.FromMinutes(Math.Max(1, alertOptions.Value.OfflineCheckIntervalMinutes));

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunCheckAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Device offline check failed");
            }

            try
            {
                await Task.Delay(interval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    private async Task RunCheckAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MonitoringDbContext>();
        var offlineEvaluator = scope.ServiceProvider.GetRequiredService<IDeviceOfflineEvaluator>();
        var alertDispatcher = scope.ServiceProvider.GetRequiredService<IAlertNotificationDispatcher>();
        var realtimeNotifier = scope.ServiceProvider.GetRequiredService<IRealtimeNotifier>();

        var alertsToNotify = await offlineEvaluator.EvaluateOfflineDevicesAsync(cancellationToken);
        if (alertsToNotify.Count == 0)
        {
            return;
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        var deviceIds = alertsToNotify.Select(a => a.DeviceId).Distinct().ToList();
        var devices = await dbContext.Devices
            .Where(d => deviceIds.Contains(d.Id))
            .ToDictionaryAsync(d => d.Id, cancellationToken);

        foreach (var alert in alertsToNotify)
        {
            if (!devices.TryGetValue(alert.DeviceId, out var device))
            {
                continue;
            }

            await alertDispatcher.DispatchAsync(device, [alert], cancellationToken);
            await realtimeNotifier.NotifyAlertsAsync(alert.CompanyId, [alert], cancellationToken);
        }
    }
}
