using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace SmartMonitoring.Shared.Audit;

public class HttpAuditPublisher(
    HttpClient httpClient,
    IOptions<AuditOptions> options,
    ILogger<HttpAuditPublisher> logger) : IAuditPublisher
{
    public async Task PublishAsync(AuditEvent auditEvent, CancellationToken cancellationToken = default)
    {
        var auditOptions = options.Value;
        if (!auditOptions.Enabled)
        {
            return;
        }

        var response = await httpClient.PostAsJsonAsync("api/audit/events", auditEvent, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning(
                "Audit publish failed with status {StatusCode} for event {EventType}",
                response.StatusCode,
                auditEvent.EventType);
        }
    }
}
