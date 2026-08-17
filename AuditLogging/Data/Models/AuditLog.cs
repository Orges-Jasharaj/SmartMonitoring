namespace AuditLogging.Data.Models;

public class AuditLog
{
    public Guid Id { get; set; }
    public string ServiceName { get; set; } = null!;
    public string EventType { get; set; } = null!;
    public string Outcome { get; set; } = null!;
    public string? ActorUserId { get; set; }
    public string? ActorUserName { get; set; }
    public string? TargetEntityType { get; set; }
    public string? TargetEntityId { get; set; }
    public string? TargetUserName { get; set; }
    public string? Detail { get; set; }
    public string? CorrelationId { get; set; }
    public string? IpAddress { get; set; }
    public DateTime OccurredAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
