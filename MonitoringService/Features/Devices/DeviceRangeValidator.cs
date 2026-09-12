namespace MonitoringService.Features.Devices;

internal static class DeviceRangeValidator
{
    public static string? ValidateAllRanges(
        decimal minTempC,
        decimal maxTempC,
        decimal minHumidityPct,
        decimal maxHumidityPct,
        decimal minCo2Ppm,
        decimal maxCo2Ppm,
        decimal minLightLevelLux,
        decimal maxLightLevelLux,
        decimal minNoiseLevelDb,
        decimal maxNoiseLevelDb)
    {
        return ValidateRange(minTempC, maxTempC, "temperature")
               ?? ValidateRange(minHumidityPct, maxHumidityPct, "humidity")
               ?? ValidateRange(minCo2Ppm, maxCo2Ppm, "CO₂")
               ?? ValidateRange(minLightLevelLux, maxLightLevelLux, "light level")
               ?? ValidateRange(minNoiseLevelDb, maxNoiseLevelDb, "noise level");
    }

    private static string? ValidateRange(decimal min, decimal max, string metricName)
    {
        return min >= max ? $"Minimum {metricName} must be less than maximum {metricName}." : null;
    }
}
