namespace SmartMonitoring.Shared.Notifications;

public class NotificationOptions
{
    public const string SectionName = "Notification";

    public bool Enabled { get; set; }
    public string BaseUrl { get; set; } = "http://localhost:8083";
    public string? ApiKey { get; set; }
}
