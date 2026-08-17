namespace SmartMonitoring.Shared.Audit;

public class AuditOptions
{
    public const string SectionName = "Audit";

    public bool Enabled { get; set; } = true;

    public string ServiceName { get; set; } = "UnknownService";

    public string BaseUrl { get; set; } = "http://localhost:8081";

    public string? ApiKey { get; set; }
}
