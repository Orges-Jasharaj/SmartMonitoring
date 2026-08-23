namespace MonitoringService.Data.Models;

public class TemperatureReading
{
    public Guid Id { get; set; }
    public Guid DeviceId { get; set; }
    public Guid CompanyId { get; set; }
    public decimal TemperatureC { get; set; }
    public DateTime MeasuredAtUtc { get; set; }
    public DateTime ReceivedAtUtc { get; set; } = DateTime.UtcNow;

    public Device Device { get; set; } = null!;
}
