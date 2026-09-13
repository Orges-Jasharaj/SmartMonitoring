using MonitoringService.Data.Models;
using MonitoringService.Features.Readings.Commands;
using Microsoft.Extensions.Options;

namespace MonitoringService.Services;

public interface IReadingPersistencePolicy
{
    bool ShouldPersist(
        Device device,
        IngestReadingCommand reading,
        TemperatureReading? lastPersistedReading,
        DateTime measuredAtUtc,
        bool forcePersist);
}

public class ReadingPersistencePolicy(IOptions<ReadingPersistenceOptions> options) : IReadingPersistencePolicy
{
    public bool ShouldPersist(
        Device device,
        IngestReadingCommand reading,
        TemperatureReading? lastPersistedReading,
        DateTime measuredAtUtc,
        bool forcePersist)
    {
        var settings = options.Value;

        if (forcePersist || !settings.Enabled)
        {
            return true;
        }

        if (lastPersistedReading == null)
        {
            return true;
        }

        if (IsOutOfRange(device, reading))
        {
            return true;
        }

        var heartbeat = TimeSpan.FromMinutes(Math.Max(1, settings.HeartbeatMinutes));
        return measuredAtUtc - lastPersistedReading.MeasuredAtUtc >= heartbeat;
    }

    internal static bool IsOutOfRange(Device device, IngestReadingCommand reading)
    {
        if (reading.TemperatureC < device.MinTempC || reading.TemperatureC > device.MaxTempC)
        {
            return true;
        }

        if (reading.HumidityPct.HasValue
            && (reading.HumidityPct.Value < device.MinHumidityPct
                || reading.HumidityPct.Value > device.MaxHumidityPct))
        {
            return true;
        }

        if (reading.Co2Ppm.HasValue
            && (reading.Co2Ppm.Value < device.MinCo2Ppm || reading.Co2Ppm.Value > device.MaxCo2Ppm))
        {
            return true;
        }

        if (reading.LightLevelLux.HasValue
            && (reading.LightLevelLux.Value < device.MinLightLevelLux
                || reading.LightLevelLux.Value > device.MaxLightLevelLux))
        {
            return true;
        }

        if (reading.NoiseLevelDb.HasValue
            && (reading.NoiseLevelDb.Value < device.MinNoiseLevelDb
                || reading.NoiseLevelDb.Value > device.MaxNoiseLevelDb))
        {
            return true;
        }

        if (reading.BatteryLevelPct.HasValue && reading.BatteryLevelPct.Value < device.MinBatteryPct)
        {
            return true;
        }

        return false;
    }
}
