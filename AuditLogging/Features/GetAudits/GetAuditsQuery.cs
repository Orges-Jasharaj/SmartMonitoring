using MediatR;
using SmartMonitoring.Shared.Dtos.Responses;

namespace AuditLogging.Features.GetAudits;

public class GetAuditsQuery : IRequest<ResponseDto<PagedAuditLogsResult>>
{
    public string? ServiceName { get; init; }
    public string? EventType { get; init; }
    public string? ActorUserId { get; init; }
    public DateTime? FromUtc { get; init; }
    public DateTime? ToUtc { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 50;
}

public class PagedAuditLogsResult
{
    public required IReadOnlyList<AuditLogDto> Items { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalCount { get; init; }
}

public class AuditLogDto
{
    public Guid Id { get; init; }
    public string ServiceName { get; init; } = null!;
    public string EventType { get; init; } = null!;
    public string Outcome { get; init; } = null!;
    public string? ActorUserId { get; init; }
    public string? ActorUserName { get; init; }
    public string? TargetEntityType { get; init; }
    public string? TargetEntityId { get; init; }
    public string? TargetUserName { get; init; }
    public string? Detail { get; init; }
    public string? CorrelationId { get; init; }
    public string? IpAddress { get; init; }
    public DateTime OccurredAtUtc { get; init; }
}
