import type { Reading } from '../api/types';
import { MetricChart } from './MetricChart';

type TemperatureChartProps = {
  readings: Reading[];
  minTempC: number;
  maxTempC: number;
};

export function TemperatureChart({ readings, minTempC, maxTempC }: TemperatureChartProps) {
  return (
    <MetricChart
      readings={readings}
      dataKey="temperatureC"
      label="Temperature"
      unit="°C"
      color="#38bdf8"
      minRange={minTempC}
      maxRange={maxTempC}
      height={280}
    />
  );
}
