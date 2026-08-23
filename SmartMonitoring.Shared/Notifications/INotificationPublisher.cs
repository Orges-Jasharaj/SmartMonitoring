namespace SmartMonitoring.Shared.Notifications;

public interface INotificationPublisher
{
    Task PublishAlertAsync(AlertNotificationRequest request, CancellationToken cancellationToken = default);
}
