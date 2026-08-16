using Microsoft.Extensions.Logging;

namespace SmartMonitoring.Shared.Observability;

public static class AuditLoggerExtensions
{
    public static void LogAuditEvent(
        this ILogger logger,
        string eventType,
        string outcome,
        string? actorUserId = null,
        string? actorUserName = null,
        string? targetUserId = null,
        string? targetUserName = null,
        string? detail = null)
    {
        logger.LogInformation(
            "Audit event {AuditEventType} completed with outcome {Outcome}. ActorUserId={ActorUserId} ActorUserName={ActorUserName} TargetUserId={TargetUserId} TargetUserName={TargetUserName} Detail={Detail}",
            eventType,
            outcome,
            actorUserId,
            actorUserName,
            targetUserId,
            targetUserName,
            detail);
    }
}
