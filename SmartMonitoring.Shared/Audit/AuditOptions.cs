namespace SmartMonitoring.Shared.Audit;

public class AuditOptions
{
    public const string SectionName = "Audit";

    public bool Enabled { get; set; } = true;

    public string ServiceName { get; set; } = "UnknownService";

    public string BaseUrl { get; set; } = "http://localhost:8081";

    public string? ApiKey { get; set; }

    /// <summary>Http (direct POST) or Kafka (publish to topic).</summary>
    public string Transport { get; set; } = "Http";
}
