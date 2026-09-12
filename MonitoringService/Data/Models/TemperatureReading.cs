namespace MonitoringService.Data.Models;

public class TemperatureReading
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
    public DateTime ReceivedAtUtc { get; set; } = DateTime.UtcNow;

    public Device Device { get; set; } = null!;
}
