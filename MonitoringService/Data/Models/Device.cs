namespace MonitoringService.Data.Models;

public class Device
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public required string Name { get; set; }
    public required string ZoneName { get; set; }
    public decimal MinTempC { get; set; }
    public decimal MaxTempC { get; set; }
    public decimal MinHumidityPct { get; set; } = 40;
    public decimal MaxHumidityPct { get; set; } = 70;
    public decimal MinCo2Ppm { get; set; } = 400;
    public decimal MaxCo2Ppm { get; set; } = 1000;
    public decimal MinLightLevelLux { get; set; } = 0;
    public decimal MaxLightLevelLux { get; set; } = 500;
    public decimal MinNoiseLevelDb { get; set; } = 35;
    public decimal MaxNoiseLevelDb { get; set; } = 70;
    public decimal MinBatteryPct { get; set; } = 20;
    public required string DeviceKey { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime? LastReadingAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public Company Company { get; set; } = null!;
    public ICollection<TemperatureReading> Readings { get; set; } = [];
    public ICollection<Alert> Alerts { get; set; } = [];
}
