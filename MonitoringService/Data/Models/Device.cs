namespace MonitoringService.Data.Models;

public class Device
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public required string Name { get; set; }
    public required string ZoneName { get; set; }
    public decimal MinTempC { get; set; }
    public decimal MaxTempC { get; set; }
    public required string DeviceKey { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime? LastReadingAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public Company Company { get; set; } = null!;
    public ICollection<TemperatureReading> Readings { get; set; } = [];
    public ICollection<Alert> Alerts { get; set; } = [];
}
