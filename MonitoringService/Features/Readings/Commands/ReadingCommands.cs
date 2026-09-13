using MediatR;
using SmartMonitoring.Shared.Dtos.Responses;

namespace MonitoringService.Features.Readings.Commands;

public class IngestReadingCommand : IRequest<ResponseDto<ReadingDto>>
{
    public string DeviceKey { get; set; } = null!;
    public decimal TemperatureC { get; set; }
    public decimal? HumidityPct { get; set; }
    public decimal? Co2Ppm { get; set; }
    public decimal? LightLevelLux { get; set; }
    public decimal? NoiseLevelDb { get; set; }
    public decimal? BatteryLevelPct { get; set; }
    public DateTime? MeasuredAtUtc { get; set; }

    /// <summary>When true, always persist (e.g. manual simulate from the UI).</summary>
    public bool ForcePersist { get; set; }
}

public class ReadingDto
{
    public Guid Id { get; set; }
    public Guid DeviceId { get; set; }
    public Guid CompanyId { get; set; }
    public decimal TemperatureC { get; set; }
    public decimal? HumidityPct { get; set; }
    public decimal? Co2Ppm { get; set; }
    public decimal? LightLevelLux { get; set; }
    public decimal? NoiseLevelDb { get; set; }
    public decimal? BatteryLevelPct { get; set; }
    public DateTime MeasuredAtUtc { get; set; }
    public DateTime ReceivedAtUtc { get; set; }
}
