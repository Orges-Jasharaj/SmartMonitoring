namespace MonitoringService.Services;

public class AlertOptions
{
    public const string SectionName = "Alert";

    /// <summary>
    /// Minimum minutes between reminder emails while a temperature alert stays active.
    /// Set to 0 to notify on every out-of-range reading (useful for local testing only).
    /// </summary>
    public int ActiveAlertReminderMinutes { get; set; } = 15;

    /// <summary>
    /// Minutes without a reading before a device is considered offline.
    /// </summary>
    public int DeviceOfflineAfterMinutes { get; set; } = 30;

    /// <summary>
    /// How often the background job scans for offline devices.
    /// </summary>
    public int OfflineCheckIntervalMinutes { get; set; } = 5;
}
