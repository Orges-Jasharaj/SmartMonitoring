import { useCallback, useEffect, useState, type FormEvent } from 'react';
import { Link, useNavigate, useParams } from 'react-router-dom';
import { api } from '../api/client';
import type { CompanyUser, Device, Reading } from '../api/types';
import { EnvironmentalCharts } from '../components/EnvironmentalCharts';
import { StatCard } from '../components/StatCard';
import { useToast } from '../components/Toast';
import { useAuth } from '../auth/AuthContext';
import { useMonitoringHub } from '../hooks/useMonitoringHub';
import { useMonitoringClock } from '../hooks/useMonitoringClock';
import { prependReading } from '../realtime/monitoringHub';
import { DeviceKeyPanel } from '../components/DeviceKeyPanel';
import { DeviceThresholdFields } from '../components/DeviceThresholdFields';
import { deviceThresholdFormFromDevice, parseDeviceThresholdPayload } from '../constants/deviceDefaults';
import {
  canManageCompanyDevices,
  formatDateTime,
  formatMetric,
  getDeviceLastReadingAt,
  getDeviceStatus,
  getLatestReading,
} from '../utils/monitoring';

function optionalNumber(value: string) {
  const trimmed = value.trim();
  if (!trimmed) return undefined;
  const parsed = Number(trimmed);
  return Number.isFinite(parsed) ? parsed : undefined;
}

export function DevicePage() {
  const { companyId = '', deviceId = '' } = useParams();
  const navigate = useNavigate();
  const { token, isAdmin, userId } = useAuth();
  const { pushToast } = useToast();
  const now = useMonitoringClock();
  const [device, setDevice] = useState<Device | null>(null);
  const [readings, setReadings] = useState<Reading[]>([]);
  const [members, setMembers] = useState<CompanyUser[]>([]);
  const [error, setError] = useState<string | null>(null);
  const [deviceKey, setDeviceKey] = useState('');
  const [simulateForm, setSimulateForm] = useState({
    temperatureC: '6',
    humidityPct: '55',
    co2Ppm: '450',
    lightLevelLux: '120',
    noiseLevelDb: '42',
    batteryLevelPct: '88',
  });
  const [simulating, setSimulating] = useState(false);
  const [savingSettings, setSavingSettings] = useState(false);
  const [settingsForm, setSettingsForm] = useState({
    name: '',
    zoneName: '',
    minTempC: '',
    maxTempC: '',
    minHumidityPct: '',
    maxHumidityPct: '',
    minCo2Ppm: '',
    maxCo2Ppm: '',
    minLightLevelLux: '',
    maxLightLevelLux: '',
    minNoiseLevelDb: '',
    maxNoiseLevelDb: '',
    minBatteryPct: '',
  });

  const refresh = useCallback(async () => {
    if (!token || !companyId || !deviceId) return;
    setError(null);

    const [deviceRes, readingsRes] = await Promise.all([
      api.getDevice(token, deviceId),
      api.getReadings(token, companyId, deviceId, 100),
    ]);

    if (deviceRes.success && deviceRes.data) {
      setDevice(deviceRes.data);
      setSettingsForm({
        name: deviceRes.data.name,
        zoneName: deviceRes.data.zoneName,
        ...deviceThresholdFormFromDevice(deviceRes.data),
      });
    } else {
      setDevice(null);
      setError(deviceRes.message ?? 'Device not found');
    }

    if (readingsRes.success && readingsRes.data) {
      setReadings(readingsRes.data);
    }
  }, [token, companyId, deviceId]);

  useEffect(() => {
    void refresh();
  }, [refresh]);

  useEffect(() => {
    if (!token || !companyId || !deviceId) return;
    const timer = window.setInterval(() => {
      void refresh();
    }, 30_000);
    return () => window.clearInterval(timer);
  }, [token, companyId, deviceId, refresh]);

  useEffect(() => {
    if (!device) return;
    const hash = window.location.hash;
    if (hash === '#device-key' || hash === '#device-settings') {
      document.getElementById(hash.slice(1))?.scrollIntoView({ behavior: 'smooth' });
    }
  }, [device]);

  useEffect(() => {
    if (!token || !companyId) return;
    void api.getCompanyUsers(token, companyId).then((response) => {
      setMembers(response.data ?? []);
    });
  }, [token, companyId]);

  const canManageDevices = canManageCompanyDevices(isAdmin, userId, members);

  const handleReading = useCallback(
    (reading: Reading) => {
      setReadings((current) => prependReading(current, reading));
      setDevice((current) =>
        current && current.id.toLowerCase() === reading.deviceId.toLowerCase()
          ? { ...current, lastReadingAtUtc: reading.measuredAtUtc }
          : current,
      );
    },
    [],
  );

  useMonitoringHub({ companyId, deviceId, onReading: handleReading });

  async function handleDeleteDevice() {
    if (!token || !companyId || !deviceId || !device) return;
    if (!window.confirm(`Delete device "${device.name}"? This also removes its readings and alerts.`)) {
      return;
    }

    const response = await api.deleteDevice(token, companyId, deviceId);
    if (!response.success) {
      pushToast(response.message ?? 'Failed to delete device', 'error');
      return;
    }

    pushToast(`Device "${device.name}" deleted`, 'success');
    navigate(`/companies/${companyId}`);
  }

  async function handleSimulateReading(event: FormEvent) {
    event.preventDefault();
    if (!deviceKey.trim()) {
      pushToast('Device key is required', 'error');
      return;
    }

    setSimulating(true);
    const response = await api.ingestReading(deviceKey.trim(), {
      temperatureC: Number(simulateForm.temperatureC),
      humidityPct: optionalNumber(simulateForm.humidityPct),
      co2Ppm: optionalNumber(simulateForm.co2Ppm),
      lightLevelLux: optionalNumber(simulateForm.lightLevelLux),
      noiseLevelDb: optionalNumber(simulateForm.noiseLevelDb),
      batteryLevelPct: optionalNumber(simulateForm.batteryLevelPct),
    });
    setSimulating(false);

    if (!response.success || !response.data) {
      pushToast(response.message ?? 'Ingest failed', 'error');
      return;
    }

    handleReading(response.data);
    pushToast('Environmental reading ingested', 'success');
  }

  async function handleUpdateSettings(event: FormEvent) {
    event.preventDefault();
    if (!token || !companyId || !deviceId) return;

    setSavingSettings(true);
    const response = await api.updateDevice(token, companyId, deviceId, {
      name: settingsForm.name.trim(),
      zoneName: settingsForm.zoneName.trim(),
      ...parseDeviceThresholdPayload(settingsForm),
    });
    setSavingSettings(false);

    if (!response.success || !response.data) {
      pushToast(response.message ?? 'Failed to update device', 'error');
      return;
    }

    setDevice(response.data);
    pushToast('Device settings saved', 'success');
  }

  if (!device && !error) {
    return <p className="muted page-loading">Loading device…</p>;
  }

  if (!device) {
    return (
      <section className="stack">
        <Link to={`/companies/${companyId}`} className="back-link">← Back to company</Link>
        <p className="error-banner">{error ?? 'Device not found'}</p>
      </section>
    );
  }

  const lastReadingAt = getDeviceLastReadingAt(device, readings);
  const status = getDeviceStatus(device, readings, now);
  const latest = getLatestReading(readings, device.id);

  return (
    <section className="stack">
      <div className="page-header">
        <div>
          <Link to={`/companies/${companyId}`} className="back-link">← Back to company</Link>
          <h1>{device.name}</h1>
          <p className="muted">
            {device.zoneName} · Temp {device.minTempC}–{device.maxTempC}°C · Humidity {device.minHumidityPct}–{device.maxHumidityPct}% · CO₂ {device.minCo2Ppm}–{device.maxCo2Ppm} ppm
          </p>
        </div>
        <div className="header-actions">
          {canManageDevices && (
            <a href="#device-key" className="btn btn-secondary">
              Device key
            </a>
          )}
          {canManageDevices && (
            <button type="button" className="btn btn-ghost" onClick={() => void handleDeleteDevice()}>
              Delete device
            </button>
          )}
          <button type="button" className="btn btn-ghost" onClick={() => void refresh()}>Refresh</button>
        </div>
      </div>

      {error && <p className="error-banner">{error}</p>}

      <div className="stat-grid">
        <StatCard
          label="Status"
          value={status.label}
          tone={status.tone === 'danger' ? 'danger' : status.tone === 'ok' ? 'ok' : status.tone === 'warning' ? 'warning' : 'default'}
          hint={
            status.tone === 'warning'
              ? `Last reading ${formatDateTime(lastReadingAt ?? device.lastReadingAtUtc)} — no new data for 30+ min`
              : undefined
          }
        />
        <StatCard label="Temperature" value={formatMetric(latest?.temperatureC, '°C')} />
        <StatCard label="Humidity" value={formatMetric(latest?.humidityPct, '%')} />
        <StatCard label="CO₂" value={formatMetric(latest?.co2Ppm, ' ppm')} />
        <StatCard label="Light" value={formatMetric(latest?.lightLevelLux, ' lux')} />
        <StatCard label="Noise" value={formatMetric(latest?.noiseLevelDb, ' dB')} />
        <StatCard label="Battery" value={formatMetric(latest?.batteryLevelPct, '%')} />
        <StatCard label="Last reading" value={formatDateTime(lastReadingAt ?? device.lastReadingAtUtc)} />
      </div>

      {canManageDevices && (
        <DeviceKeyPanel
          companyId={companyId}
          deviceId={deviceId}
          deviceKey={deviceKey}
          onDeviceKeyChange={setDeviceKey}
        />
      )}

      {canManageDevices && (
        <form id="device-settings" className="card stack" onSubmit={handleUpdateSettings}>
          <h2>Device settings</h2>
          <p className="muted small">Update identity, zone, and alert thresholds for all sensors.</p>
          <label>
            Name
            <input
              value={settingsForm.name}
              onChange={(e) => setSettingsForm((f) => ({ ...f, name: e.target.value }))}
              required
            />
          </label>
          <label>
            Zone
            <input
              value={settingsForm.zoneName}
              onChange={(e) => setSettingsForm((f) => ({ ...f, zoneName: e.target.value }))}
              required
            />
          </label>
          <DeviceThresholdFields
            form={settingsForm}
            onChange={(next) => setSettingsForm((current) => ({ ...current, ...next }))}
          />
          <button type="submit" className="btn btn-primary" disabled={savingSettings}>
            {savingSettings ? 'Saving…' : 'Save settings'}
          </button>
        </form>
      )}

      {canManageDevices && (
      <div className="card stack">
        <h2>Simulate IoT reading</h2>
        <p className="muted small">
          Show the device key above first, then send a simulated environmental payload. Leave optional fields blank to omit them.
        </p>
        <form className="stack" onSubmit={handleSimulateReading}>
          <label>
            X-Device-Key
            <input value={deviceKey} onChange={(e) => setDeviceKey(e.target.value)} placeholder="Show device key above or paste here" required />
          </label>
          <div className="inline-fields">
            <label>
              Temperature (°C)
              <input type="number" step="0.1" value={simulateForm.temperatureC} onChange={(e) => setSimulateForm((f) => ({ ...f, temperatureC: e.target.value }))} required />
            </label>
            <label>
              Humidity (%)
              <input type="number" step="0.1" value={simulateForm.humidityPct} onChange={(e) => setSimulateForm((f) => ({ ...f, humidityPct: e.target.value }))} />
            </label>
          </div>
          <div className="inline-fields">
            <label>
              CO₂ (ppm)
              <input type="number" step="1" value={simulateForm.co2Ppm} onChange={(e) => setSimulateForm((f) => ({ ...f, co2Ppm: e.target.value }))} />
            </label>
            <label>
              Light (lux)
              <input type="number" step="1" value={simulateForm.lightLevelLux} onChange={(e) => setSimulateForm((f) => ({ ...f, lightLevelLux: e.target.value }))} />
            </label>
          </div>
          <div className="inline-fields">
            <label>
              Noise (dB)
              <input type="number" step="0.1" value={simulateForm.noiseLevelDb} onChange={(e) => setSimulateForm((f) => ({ ...f, noiseLevelDb: e.target.value }))} />
            </label>
            <label>
              Battery (%)
              <input type="number" step="1" value={simulateForm.batteryLevelPct} onChange={(e) => setSimulateForm((f) => ({ ...f, batteryLevelPct: e.target.value }))} />
            </label>
          </div>
          <button type="submit" className="btn btn-secondary" disabled={simulating || !deviceKey.trim()}>
            {simulating ? 'Sending…' : 'Send reading'}
          </button>
        </form>
      </div>
      )}

      <div className="card stack">
        <h2>Environmental history</h2>
        <p className="muted small">Green bands show each sensor&apos;s configured safe range.</p>
        <EnvironmentalCharts device={device} readings={readings} />
      </div>

      <div className="card stack">
        <h2>Recent readings</h2>
        {readings.length === 0 && <p className="muted">No readings for this device yet.</p>}
        <div className="table-wrap">
          <table>
            <thead>
              <tr>
                <th>Temp</th>
                <th>Humidity</th>
                <th>CO₂</th>
                <th>Light</th>
                <th>Noise</th>
                <th>Battery</th>
                <th>Measured</th>
              </tr>
            </thead>
            <tbody>
              {readings.map((reading) => (
                <tr key={reading.id}>
                  <td>{reading.temperatureC}°C</td>
                  <td>{formatMetric(reading.humidityPct, '%', '—')}</td>
                  <td>{formatMetric(reading.co2Ppm, ' ppm', '—')}</td>
                  <td>{formatMetric(reading.lightLevelLux, ' lux', '—')}</td>
                  <td>{formatMetric(reading.noiseLevelDb, ' dB', '—')}</td>
                  <td>{formatMetric(reading.batteryLevelPct, '%', '—')}</td>
                  <td>{formatDateTime(reading.measuredAtUtc)}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </div>
    </section>
  );
}
