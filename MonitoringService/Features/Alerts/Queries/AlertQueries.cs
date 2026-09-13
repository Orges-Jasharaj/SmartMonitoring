using MediatR;
using SmartMonitoring.Shared.Dtos;
using SmartMonitoring.Shared.Dtos.Responses;

namespace MonitoringService.Features.Alerts.Queries;

public class GetAlertsQuery : IRequest<ResponseDto<PagedResult<AlertDto>>>
{
    public Guid CompanyId { get; set; }
    public bool ActiveOnly { get; set; } = true;
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = Pagination.DefaultPageSize;
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
