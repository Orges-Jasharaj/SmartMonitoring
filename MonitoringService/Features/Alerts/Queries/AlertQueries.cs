using MediatR;
using SmartMonitoring.Shared.Dtos.Responses;

namespace MonitoringService.Features.Alerts.Queries;

public class GetAlertsQuery : IRequest<ResponseDto<IReadOnlyList<AlertDto>>>
{
    public Guid CompanyId { get; set; }
    public bool ActiveOnly { get; set; } = true;
}

public class AlertDto
{
    public Guid Id { get; set; }
    public Guid DeviceId { get; set; }
    public Guid CompanyId { get; set; }
    public string AlertType { get; set; } = null!;
    public string Message { get; set; } = null!;
    public decimal? TemperatureC { get; set; }
    public DateTime TriggeredAtUtc { get; set; }
    public DateTime? ResolvedAtUtc { get; set; }
    public bool IsActive { get; set; }
}
