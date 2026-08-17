namespace SmartMonitoring.Shared.Audit;

public interface IAuditPublisher
{
    Task PublishAsync(AuditEvent auditEvent, CancellationToken cancellationToken = default);
}
