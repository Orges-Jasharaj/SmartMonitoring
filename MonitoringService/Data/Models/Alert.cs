namespace MonitoringService.Data.Models;

public class Alert
{
    public Guid Id { get; set; }
    public Guid DeviceId { get; set; }
    public Guid CompanyId { get; set; }
    public required string AlertType { get; set; }
    public required string Message { get; set; }
    public decimal? TemperatureC { get; set; }
    public DateTime TriggeredAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? ResolvedAtUtc { get; set; }
    public bool IsActive { get; set; } = true;

    public Device Device { get; set; } = null!;
}
