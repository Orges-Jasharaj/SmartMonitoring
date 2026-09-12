import type { Device, Reading } from '../api/types';
import { MetricChart } from './MetricChart';

type Props = {
  device: Device;
  readings: Reading[];
  compact?: boolean;
};

export function EnvironmentalCharts({ device, readings, compact = false }: Props) {
  const height = compact ? 180 : 220;

  return (
    <div className="chart-grid">
      <div className="chart-panel">
        <h3>Temperature</h3>
        <MetricChart
          readings={readings}
          dataKey="temperatureC"
          label="Temperature"
          unit="°C"
          color="#38bdf8"
          minRange={device.minTempC}
          maxRange={device.maxTempC}
          height={height}
        />
      </div>
      <div className="chart-panel">
        <h3>Humidity</h3>
        <MetricChart
          readings={readings}
          dataKey="humidityPct"
          label="Humidity"
          unit="%"
          color="#22d3ee"
          minRange={device.minHumidityPct}
          maxRange={device.maxHumidityPct}
          height={height}
        />
      </div>
      <div className="chart-panel">
        <h3>CO₂</h3>
        <MetricChart
          readings={readings}
          dataKey="co2Ppm"
          label="CO₂"
          unit=" ppm"
          color="#a78bfa"
          minRange={device.minCo2Ppm}
          maxRange={device.maxCo2Ppm}
          height={height}
        />
      </div>
      <div className="chart-panel">
        <h3>Light</h3>
        <MetricChart
          readings={readings}
          dataKey="lightLevelLux"
          label="Light"
          unit=" lux"
          color="#fbbf24"
          minRange={device.minLightLevelLux}
          maxRange={device.maxLightLevelLux}
          height={height}
        />
      </div>
      <div className="chart-panel">
        <h3>Noise</h3>
        <MetricChart
          readings={readings}
          dataKey="noiseLevelDb"
          label="Noise"
          unit=" dB"
          color="#fb7185"
          minRange={device.minNoiseLevelDb}
          maxRange={device.maxNoiseLevelDb}
          height={height}
        />
      </div>
      <div className="chart-panel">
        <h3>Battery</h3>
        <MetricChart
          readings={readings}
          dataKey="batteryLevelPct"
          label="Battery"
          unit="%"
          color="#4ade80"
          minRange={device.minBatteryPct}
          maxRange={100}
          height={height}
        />
      </div>
    </div>
  );
}
