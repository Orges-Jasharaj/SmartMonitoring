using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SmartMonitoring.Shared.Middleware;
using SmartMonitoring.Shared.Observability;

namespace SmartMonitoring.Shared.Audit;

public class AuditRecorder(
    ILogger<AuditRecorder> logger,
    IAuditPublisher auditPublisher,
    IHttpContextAccessor httpContextAccessor,
    IOptions<AuditOptions> options) : IAuditRecorder
{
    public async Task RecordAsync(
        string eventType,
        string outcome,
        string? actorUserId = null,
        string? actorUserName = null,
        string? targetEntityType = null,
        string? targetEntityId = null,
        string? targetUserName = null,
        string? detail = null,
        CancellationToken cancellationToken = default)
    {
        logger.LogAuditEvent(
            eventType,
            outcome,
            actorUserId,
            actorUserName,
            targetEntityId,
            targetUserName,
            detail);

        if (!options.Value.Enabled)
        {
            return;
        }

        var httpContext = httpContextAccessor.HttpContext;
        var correlationId = httpContext?.Items[CorrelationIdMiddleware.ItemKey]?.ToString()
            ?? httpContext?.TraceIdentifier;

        var auditEvent = new AuditEvent
        {
            ServiceName = options.Value.ServiceName,
            EventType = eventType,
            Outcome = outcome,
            ActorUserId = actorUserId,
            ActorUserName = actorUserName,
            TargetEntityType = targetEntityType,
            TargetEntityId = targetEntityId,
            TargetUserName = targetUserName,
            Detail = detail,
            CorrelationId = correlationId,
            IpAddress = httpContext?.Connection.RemoteIpAddress?.ToString(),
            OccurredAtUtc = DateTime.UtcNow
        };

        try
        {
            await auditPublisher.PublishAsync(auditEvent, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to publish audit event {EventType} to AuditLogging service", eventType);
        }
    }
}
