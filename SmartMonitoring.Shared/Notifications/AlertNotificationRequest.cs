namespace SmartMonitoring.Shared.Notifications;

public class AlertNotificationRequest
{
    public Guid AlertId { get; set; }
    public Guid CompanyId { get; set; }
    public Guid DeviceId { get; set; }
    public string DeviceName { get; set; } = null!;
    public string ZoneName { get; set; } = null!;
    public string AlertType { get; set; } = null!;
    public string Message { get; set; } = null!;
    public decimal? TemperatureC { get; set; }
    public DateTime TriggeredAtUtc { get; set; }
    public List<string> RecipientEmails { get; set; } = [];
}
