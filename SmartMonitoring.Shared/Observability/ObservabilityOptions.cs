namespace SmartMonitoring.Shared.Observability;

public class ObservabilityOptions
{
    public const string SectionName = "Observability";

    public string ServiceName { get; set; } = "UnknownService";

    public bool EnableLoki { get; set; } = true;

    public string LokiUrl { get; set; } = "http://localhost:3100";

    public bool EnableOpenTelemetry { get; set; } = true;

    public string OtlpEndpoint { get; set; } = "http://localhost:4317";

    public bool EnablePrometheus { get; set; } = true;

    public bool RestrictMetricsToLocalhost { get; set; } = true;

    public bool EnableRequestLogging { get; set; } = true;
}
