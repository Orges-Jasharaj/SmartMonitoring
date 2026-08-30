using MediatR;
using MonitoringService.Features.Alerts.Queries;
using SmartMonitoring.Shared.Dtos.Responses;

namespace MonitoringService.Features.Alerts.Commands;

public class AcknowledgeAlertCommand : IRequest<ResponseDto<AlertDto>>
{
    public Guid CompanyId { get; set; }
    public Guid AlertId { get; set; }
}
