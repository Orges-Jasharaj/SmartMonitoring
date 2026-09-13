namespace AuditLogging.Services;

public class AuditRetentionOptions
{
    public const string SectionName = "AuditRetention";

    public bool Enabled { get; set; } = true;

    public int RetentionDays { get; set; } = 90;

    public int CleanupIntervalHours { get; set; } = 24;
}
