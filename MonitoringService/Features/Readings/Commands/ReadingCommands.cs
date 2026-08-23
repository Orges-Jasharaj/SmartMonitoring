using MediatR;
using SmartMonitoring.Shared.Dtos.Responses;

namespace MonitoringService.Features.Readings.Commands;

public class IngestReadingCommand : IRequest<ResponseDto<ReadingDto>>
{
    public string DeviceKey { get; set; } = null!;
    public decimal TemperatureC { get; set; }
    public DateTime? MeasuredAtUtc { get; set; }
}

public class ReadingDto
{
    public Guid Id { get; set; }
    public Guid DeviceId { get; set; }
    public Guid CompanyId { get; set; }
    public decimal TemperatureC { get; set; }
    public DateTime MeasuredAtUtc { get; set; }
    public DateTime ReceivedAtUtc { get; set; }
}
