using MediatR;
using MonitoringService.Features.Readings.Commands;
using SmartMonitoring.Shared.Dtos.Responses;

namespace MonitoringService.Features.Readings.Queries;

public class GetReadingsQuery : IRequest<ResponseDto<IReadOnlyList<ReadingDto>>>
{
    public Guid CompanyId { get; set; }
    public Guid? DeviceId { get; set; }
    public DateTime? FromUtc { get; set; }
    public DateTime? ToUtc { get; set; }
    public int Limit { get; set; } = 100;
}
