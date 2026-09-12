using System.Text.Json;
using System.Text.Json.Serialization;

namespace SmartMonitoring.Shared.Json;

public static class JsonSerializerOptionsExtensions
{
    public static void ApplySmartMonitoringDefaults(this JsonSerializerOptions options)
    {
        options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.Converters.Add(new UtcDateTimeConverter());
        options.Converters.Add(new NullableUtcDateTimeConverter());
    }
}
