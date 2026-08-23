import {
  CartesianGrid,
  Line,
  LineChart,
  ReferenceArea,
  ResponsiveContainer,
  Tooltip,
  XAxis,
  YAxis,
} from 'recharts';
import type { Reading } from '../api/types';

type TemperatureChartProps = {
  readings: Reading[];
  minTempC: number;
  maxTempC: number;
};

export function TemperatureChart({ readings, minTempC, maxTempC }: TemperatureChartProps) {
  const data = [...readings]
    .sort((a, b) => new Date(a.measuredAtUtc).getTime() - new Date(b.measuredAtUtc).getTime())
    .map((reading) => ({
      time: new Date(reading.measuredAtUtc).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' }),
      temperatureC: reading.temperatureC,
    }));

  if (data.length === 0) {
    return <p className="muted chart-empty">No readings to chart yet.</p>;
  }

  const temps = data.map((point) => point.temperatureC);
  const yMin = Math.min(minTempC - 2, ...temps);
  const yMax = Math.max(maxTempC + 2, ...temps);

  return (
    <div className="chart-wrap">
      <ResponsiveContainer width="100%" height={280}>
        <LineChart data={data} margin={{ top: 8, right: 12, left: 0, bottom: 0 }}>
          <CartesianGrid stroke="rgba(148,163,184,0.15)" strokeDasharray="4 4" />
          <XAxis dataKey="time" stroke="#93a4c3" tick={{ fontSize: 12 }} />
          <YAxis domain={[yMin, yMax]} stroke="#93a4c3" tick={{ fontSize: 12 }} unit="°C" />
          <Tooltip
            contentStyle={{
              background: '#121b2e',
              border: '1px solid #24314d',
              borderRadius: '0.65rem',
            }}
            formatter={(value) => [`${value}°C`, 'Temperature']}
          />
          <ReferenceArea y1={minTempC} y2={maxTempC} fill="rgba(34,197,94,0.08)" strokeOpacity={0} />
          <Line
            type="monotone"
            dataKey="temperatureC"
            stroke="#38bdf8"
            strokeWidth={2}
            dot={{ r: 3, fill: '#38bdf8' }}
            activeDot={{ r: 5 }}
          />
        </LineChart>
      </ResponsiveContainer>
      <div className="chart-legend">
        <span className="legend-safe">Safe range {minTempC}°C – {maxTempC}°C</span>
      </div>
    </div>
  );
}
