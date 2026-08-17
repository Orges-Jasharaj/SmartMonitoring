using SmartMonitoring.Shared.Audit;

namespace AuditLogging.Features.RecordAudit;

public class RecordAuditCommand : MediatR.IRequest<SmartMonitoring.Shared.Dtos.Responses.ResponseDto<Guid>>
{
    public required AuditEvent AuditEvent { get; init; }
}
