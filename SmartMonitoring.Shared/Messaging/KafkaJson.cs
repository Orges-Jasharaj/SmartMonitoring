using System.Text.Json;
using SmartMonitoring.Shared.Json;

namespace SmartMonitoring.Shared.Messaging;

public static class KafkaJson
{
    public static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    static KafkaJson()
    {
        SerializerOptions.ApplySmartMonitoringDefaults();
    }
}
