namespace MonitoringService.Services;

public class ReadingPersistenceOptions
{
    public const string SectionName = "ReadingPersistence";

    public bool Enabled { get; set; } = true;

    /// <summary>When in range, persist at most one reading per interval.</summary>
    public int HeartbeatMinutes { get; set; } = 5;
}
