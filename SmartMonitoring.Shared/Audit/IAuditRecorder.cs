namespace SmartMonitoring.Shared.Audit;

public interface IAuditRecorder
{
    Task RecordAsync(
        string eventType,
        string outcome,
        string? actorUserId = null,
        string? actorUserName = null,
        string? targetEntityType = null,
        string? targetEntityId = null,
        string? targetUserName = null,
        string? detail = null,
        CancellationToken cancellationToken = default);
}
