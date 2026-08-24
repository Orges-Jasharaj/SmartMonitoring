using Microsoft.AspNetCore.SignalR;
using MonitoringService.Data.Models;
using MonitoringService.Features.Alerts.Queries;
using MonitoringService.Features.Readings.Commands;
using MonitoringService.Hubs;

namespace MonitoringService.Services;

public class RealtimeNotifier(IHubContext<MonitoringHub> hubContext) : IRealtimeNotifier
{
    public Task NotifyReadingAsync(ReadingDto reading, CancellationToken cancellationToken = default)
    {
        return hubContext.Clients
            .Group(MonitoringHub.CompanyGroup(reading.CompanyId))
            .SendAsync("ReadingReceived", reading, cancellationToken);
    }

    public async Task NotifyAlertsAsync(Guid companyId, IReadOnlyList<Alert> alerts, CancellationToken cancellationToken = default)
    {
        if (alerts.Count == 0)
        {
            return;
        }

        var group = hubContext.Clients.Group(MonitoringHub.CompanyGroup(companyId));
        foreach (var alert in alerts)
        {
            await group.SendAsync("AlertChanged", MapAlert(alert), cancellationToken);
        }
    }

    private static AlertDto MapAlert(Alert alert) => new()
    {
        Id = alert.Id,
        DeviceId = alert.DeviceId,
        CompanyId = alert.CompanyId,
        AlertType = alert.AlertType,
        Message = alert.Message,
        TemperatureC = alert.TemperatureC,
        TriggeredAtUtc = alert.TriggeredAtUtc,
        ResolvedAtUtc = alert.ResolvedAtUtc,
        IsActive = alert.IsActive
    };
}
