using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace SmartMonitoring.Shared.Notifications;

public class HttpNotificationPublisher(
    HttpClient httpClient,
    IOptions<NotificationOptions> options,
    ILogger<HttpNotificationPublisher> logger) : INotificationPublisher
{
    public async Task PublishAlertAsync(AlertNotificationRequest request, CancellationToken cancellationToken = default)
    {
        if (!options.Value.Enabled)
        {
            return;
        }

        var response = await httpClient.PostAsJsonAsync("api/notifications/alert", request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning(
                "Notification publish failed with status {StatusCode} for alert {AlertId}",
                response.StatusCode,
                request.AlertId);
            response.EnsureSuccessStatusCode();
        }
    }
}
