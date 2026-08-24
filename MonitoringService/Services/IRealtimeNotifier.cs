using MonitoringService.Data.Models;
using MonitoringService.Features.Alerts.Queries;
using MonitoringService.Features.Readings.Commands;

namespace MonitoringService.Services;

public interface IRealtimeNotifier
{
    Task NotifyReadingAsync(ReadingDto reading, CancellationToken cancellationToken = default);

    Task NotifyAlertsAsync(Guid companyId, IReadOnlyList<Alert> alerts, CancellationToken cancellationToken = default);
}
