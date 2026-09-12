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
import { parseUtcDateTime } from '../utils/monitoring';

type MetricChartProps = {
  readings: Reading[];
  dataKey: keyof Reading;
  label: string;
  unit: string;
  color: string;
  minRange?: number;
  maxRange?: number;
  height?: number;
};

function readMetricValue(reading: Reading, dataKey: keyof Reading) {
  const value = reading[dataKey];
  return typeof value === 'number' ? value : null;
}

export function MetricChart({
  readings,
  dataKey,
  label,
  unit,
  color,
  minRange,
  maxRange,
  height = 220,
}: MetricChartProps) {
  const data = [...readings]
    .sort(
      (a, b) =>
        (parseUtcDateTime(a.measuredAtUtc)?.getTime() ?? 0) -
        (parseUtcDateTime(b.measuredAtUtc)?.getTime() ?? 0),
    )
    .map((reading) => ({
      time:
        parseUtcDateTime(reading.measuredAtUtc)?.toLocaleTimeString([], {
          hour: '2-digit',
          minute: '2-digit',
        }) ?? '',
      value: readMetricValue(reading, dataKey),
    }))
    .filter((point) => point.value != null);

  if (data.length === 0) {
    return <p className="muted chart-empty">No {label.toLowerCase()} readings yet.</p>;
  }

  const values = data.map((point) => point.value as number);
  const hasRange = minRange != null && maxRange != null;
  const padding = Math.max(1, (Math.max(...values) - Math.min(...values)) * 0.15);
  const yMin = hasRange ? Math.min(minRange, ...values) - padding : Math.min(...values) - padding;
  const yMax = hasRange ? Math.max(maxRange, ...values) + padding : Math.max(...values) + padding;

  return (
    <div className="chart-wrap">
      <ResponsiveContainer width="100%" height={height}>
        <LineChart data={data} margin={{ top: 8, right: 12, left: 0, bottom: 0 }}>
          <CartesianGrid stroke="rgba(148,163,184,0.15)" strokeDasharray="4 4" />
          <XAxis dataKey="time" stroke="#93a4c3" tick={{ fontSize: 11 }} />
          <YAxis domain={[yMin, yMax]} stroke="#93a4c3" tick={{ fontSize: 11 }} unit={unit} />
          <Tooltip
            contentStyle={{
              background: '#121b2e',
              border: '1px solid #24314d',
              borderRadius: '0.65rem',
            }}
            formatter={(value) => [`${value}${unit}`, label]}
          />
          {hasRange && (
            <ReferenceArea y1={minRange} y2={maxRange} fill="rgba(34,197,94,0.08)" strokeOpacity={0} />
          )}
          <Line
            type="monotone"
            dataKey="value"
            stroke={color}
            strokeWidth={2}
            dot={{ r: 2, fill: color }}
            activeDot={{ r: 4 }}
            connectNulls={false}
          />
        </LineChart>
      </ResponsiveContainer>
      {hasRange && (
        <div className="chart-legend">
          <span className="legend-safe">
            Safe range {minRange}
            {unit} – {maxRange}
            {unit}
          </span>
        </div>
      )}
    </div>
  );
}
