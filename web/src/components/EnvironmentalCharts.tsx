import { useCallback, useEffect, useMemo, useState } from 'react';
import type { Device, Reading } from '../api/types';
import { MetricChart } from './MetricChart';

type Props = {
  device: Device;
  readings: Reading[];
  compact?: boolean;
};

type ChartConfig = {
  id: string;
  title: string;
  dataKey: keyof Reading;
  label: string;
  unit: string;
  color: string;
  minRange?: number;
  maxRange?: number;
};

function ExpandIcon() {
  return (
    <svg viewBox="0 0 24 24" width="18" height="18" aria-hidden="true">
      <path
        fill="currentColor"
        d="M5 5h5v2H8.4L11 9.6 9.6 11 7 8.4V10H5V5Zm14 0v5h-2V8.4L14.4 11 13 9.6 15.6 7H14V5h5ZM8.4 15 11 17.6 9.6 19 7 16.4V18H5v-5h5v2H8.4Zm7.6 0H14v-2h1.6L13 15.6l1.4 1.4L17.6 14H16v-2h5v5h-2v-1.6L15.6 19 14 17.6 15.6 16Z"
      />
    </svg>
  );
}

function CloseIcon() {
  return (
    <svg viewBox="0 0 24 24" width="20" height="20" aria-hidden="true">
      <path
        fill="currentColor"
        d="M6.4 5 12 10.6 17.6 5 19 6.4 13.4 12 19 17.6 17.6 19 12 13.4 6.4 19 5 17.6 10.6 12 5 6.4 6.4 5Z"
      />
    </svg>
  );
}

function buildCharts(device: Device): ChartConfig[] {
  return [
    {
      id: 'temperature',
      title: 'Temperature',
      dataKey: 'temperatureC',
      label: 'Temperature',
      unit: '°C',
      color: '#38bdf8',
      minRange: device.minTempC,
      maxRange: device.maxTempC,
    },
    {
      id: 'humidity',
      title: 'Humidity',
      dataKey: 'humidityPct',
      label: 'Humidity',
      unit: '%',
      color: '#22d3ee',
      minRange: device.minHumidityPct,
      maxRange: device.maxHumidityPct,
    },
    {
      id: 'co2',
      title: 'CO₂',
      dataKey: 'co2Ppm',
      label: 'CO₂',
      unit: ' ppm',
      color: '#a78bfa',
      minRange: device.minCo2Ppm,
      maxRange: device.maxCo2Ppm,
    },
    {
      id: 'light',
      title: 'Light',
      dataKey: 'lightLevelLux',
      label: 'Light',
      unit: ' lux',
      color: '#fbbf24',
      minRange: device.minLightLevelLux,
      maxRange: device.maxLightLevelLux,
    },
    {
      id: 'noise',
      title: 'Noise',
      dataKey: 'noiseLevelDb',
      label: 'Noise',
      unit: ' dB',
      color: '#fb7185',
      minRange: device.minNoiseLevelDb,
      maxRange: device.maxNoiseLevelDb,
    },
    {
      id: 'battery',
      title: 'Battery',
      dataKey: 'batteryLevelPct',
      label: 'Battery',
      unit: '%',
      color: '#4ade80',
      minRange: device.minBatteryPct,
      maxRange: 100,
    },
  ];
}

export function EnvironmentalCharts({ device, readings, compact = false }: Props) {
  const charts = useMemo(() => buildCharts(device), [device]);
  const [expandedId, setExpandedId] = useState<string | null>(null);
  const [fullscreenHeight, setFullscreenHeight] = useState(480);

  const panelHeight = compact ? 180 : 220;
  const expandedChart = charts.find((chart) => chart.id === expandedId) ?? null;

  const updateFullscreenHeight = useCallback(() => {
    setFullscreenHeight(Math.max(320, window.innerHeight - 140));
  }, []);

  useEffect(() => {
    if (!expandedId) return;

    updateFullscreenHeight();
    const previousOverflow = document.body.style.overflow;
    document.body.style.overflow = 'hidden';

    const onKeyDown = (event: KeyboardEvent) => {
      if (event.key === 'Escape') {
        setExpandedId(null);
      }
    };

    window.addEventListener('resize', updateFullscreenHeight);
    window.addEventListener('keydown', onKeyDown);

    return () => {
      document.body.style.overflow = previousOverflow;
      window.removeEventListener('resize', updateFullscreenHeight);
      window.removeEventListener('keydown', onKeyDown);
    };
  }, [expandedId, updateFullscreenHeight]);

  function renderChart(chart: ChartConfig, height: number) {
    return (
      <MetricChart
        readings={readings}
        dataKey={chart.dataKey}
        label={chart.label}
        unit={chart.unit}
        color={chart.color}
        minRange={chart.minRange}
        maxRange={chart.maxRange}
        height={height}
      />
    );
  }

  return (
    <>
      <div className="chart-grid">
        {charts.map((chart) => (
          <div key={chart.id} className="chart-panel">
            <div className="chart-panel-header">
              <h3>{chart.title}</h3>
              <button
                type="button"
                className="btn btn-ghost btn-sm chart-expand-btn"
                aria-label={`View ${chart.title} chart full screen`}
                onClick={() => setExpandedId(chart.id)}
              >
                <ExpandIcon />
              </button>
            </div>
            {renderChart(chart, panelHeight)}
          </div>
        ))}
      </div>

      {expandedChart && (
        <div
          className="chart-fullscreen-overlay"
          role="dialog"
          aria-modal="true"
          aria-label={`${expandedChart.title} chart`}
          onClick={() => setExpandedId(null)}
        >
          <div className="chart-fullscreen-inner" onClick={(event) => event.stopPropagation()}>
            <div className="chart-fullscreen-header">
              <h2>{expandedChart.title}</h2>
              <button
                type="button"
                className="btn btn-ghost btn-sm chart-close-btn"
                aria-label="Close full screen chart"
                onClick={() => setExpandedId(null)}
              >
                <CloseIcon />
              </button>
            </div>
            {renderChart(expandedChart, fullscreenHeight)}
          </div>
        </div>
      )}
    </>
  );
}
