namespace MonitoringService.Services;

public class ReadingRetentionOptions
{
    public const string SectionName = "ReadingRetention";

    /// <summary>
    /// When false, the cleanup job does not run.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Delete readings with MeasuredAtUtc older than this many days.
    /// Set to 0 or less to disable deletion while keeping the job registered.
    /// </summary>
    public int RetentionDays { get; set; } = 90;

    /// <summary>
    /// How often the cleanup job runs.
    /// </summary>
    public int CleanupIntervalHours { get; set; } = 24;
}
