using MediatR;
using MonitoringService.Features.Readings.Commands;
using SmartMonitoring.Shared.Dtos;
using SmartMonitoring.Shared.Dtos.Responses;

namespace MonitoringService.Features.Readings.Queries;

public class GetReadingsQuery : IRequest<ResponseDto<PagedResult<ReadingDto>>>
{
    public Guid CompanyId { get; set; }
    public Guid? DeviceId { get; set; }
    public DateTime? FromUtc { get; set; }
    public DateTime? ToUtc { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = Pagination.DefaultPageSize;
}
