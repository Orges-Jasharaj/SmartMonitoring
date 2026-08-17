using AuditLogging.Data;
using AuditLogging.Data.Models;
using MediatR;
using SmartMonitoring.Shared.Dtos.Responses;

namespace AuditLogging.Features.RecordAudit;

public class RecordAuditHandler(AuditDbContext dbContext) : IRequestHandler<RecordAuditCommand, ResponseDto<Guid>>
{
    public async Task<ResponseDto<Guid>> Handle(RecordAuditCommand request, CancellationToken cancellationToken)
    {
        var source = request.AuditEvent;

        var auditLog = new AuditLog
        {
            Id = Guid.NewGuid(),
            ServiceName = source.ServiceName,
            EventType = source.EventType,
            Outcome = source.Outcome,
            ActorUserId = source.ActorUserId,
            ActorUserName = source.ActorUserName,
            TargetEntityType = source.TargetEntityType,
            TargetEntityId = source.TargetEntityId,
            TargetUserName = source.TargetUserName,
            Detail = source.Detail,
            CorrelationId = source.CorrelationId,
            IpAddress = source.IpAddress,
            OccurredAtUtc = source.OccurredAtUtc == default ? DateTime.UtcNow : source.OccurredAtUtc
        };

        dbContext.AuditLogs.Add(auditLog);
        await dbContext.SaveChangesAsync(cancellationToken);

        return ResponseDto<Guid>.SuccessResponse(auditLog.Id, "Audit event recorded");
    }
}
