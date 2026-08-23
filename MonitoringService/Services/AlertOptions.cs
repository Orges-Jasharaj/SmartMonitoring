namespace MonitoringService.Services;

public class AlertOptions
{
    public const string SectionName = "Alert";

    /// <summary>
    /// Minimum minutes between reminder emails while a temperature alert stays active.
    /// Set to 0 to notify on every out-of-range reading (useful for local testing only).
    /// </summary>
    public int ActiveAlertReminderMinutes { get; set; } = 15;
}
