import { useCallback, useEffect, useState, type FormEvent } from 'react';
import { Link, useParams } from 'react-router-dom';
import { api } from '../api/client';
import type { Device, Reading } from '../api/types';
import { TemperatureChart } from '../components/TemperatureChart';
import { StatCard } from '../components/StatCard';
import { useToast } from '../components/Toast';
import { useAuth } from '../auth/AuthContext';
import { formatDateTime, getDeviceStatus } from '../utils/monitoring';

export function DevicePage() {
  const { companyId = '', deviceId = '' } = useParams();
  const { token } = useAuth();
  const { pushToast } = useToast();
  const [device, setDevice] = useState<Device | null>(null);
  const [readings, setReadings] = useState<Reading[]>([]);
  const [error, setError] = useState<string | null>(null);
  const [deviceKey, setDeviceKey] = useState('');
  const [simulateTemp, setSimulateTemp] = useState('6');
  const [simulating, setSimulating] = useState(false);
  const refresh = useCallback(async () => {
    if (!token || !companyId || !deviceId) return;

    const [deviceRes, readingsRes] = await Promise.all([
      api.getDevice(token, deviceId),
      api.getReadings(token, companyId, deviceId, 100),
    ]);

    if (deviceRes.success && deviceRes.data) {
      setDevice(deviceRes.data);
    } else {
      setError(deviceRes.message ?? 'Device not found');
    }

    if (readingsRes.success && readingsRes.data) {
      setReadings(readingsRes.data);
    }
  }, [token, companyId, deviceId]);

  useEffect(() => {
    void refresh();
  }, [refresh]);

  async function handleSimulateReading(event: FormEvent) {
    event.preventDefault();
    if (!deviceKey.trim()) {
      pushToast('Device key is required', 'error');
      return;
    }

    setSimulating(true);
    const response = await api.ingestReading(deviceKey.trim(), Number(simulateTemp));
    setSimulating(false);

    if (!response.success) {
      pushToast(response.message ?? 'Ingest failed', 'error');
      return;
    }

    pushToast(`Reading ${simulateTemp}°C ingested`, 'success');
    await refresh();
  }

  if (!device) {
    return <p className="muted page-loading">{error ?? 'Loading device…'}</p>;
  }

  const status = getDeviceStatus(device, readings);

  return (
    <section className="stack">
      <div className="page-header">
        <div>
          <Link to={`/companies/${companyId}`} className="back-link">← Back to company</Link>
          <h1>{device.name}</h1>
          <p className="muted">{device.zoneName} · Safe range {device.minTempC}°C – {device.maxTempC}°C</p>
        </div>
        <div className="header-actions">
          <button type="button" className="btn btn-ghost" onClick={() => void refresh()}>Refresh</button>
        </div>
      </div>

      {error && <p className="error-banner">{error}</p>}

      <div className="stat-grid">
        <StatCard label="Status" value={status.label} tone={status.tone === 'danger' ? 'danger' : status.tone === 'ok' ? 'ok' : 'default'} />
        <StatCard label="Latest temp" value={status.latestTemp !== undefined ? `${status.latestTemp}°C` : '—'} />
        <StatCard label="Readings loaded" value={readings.length} />
        <StatCard label="Last reading" value={formatDateTime(device.lastReadingAtUtc)} />
      </div>

      <div className="card stack">
        <h2>Simulate IoT reading</h2>
        <p className="muted small">
          Test ingest without Postman. Paste the device key saved when this device was created.
        </p>
        <form className="stack" onSubmit={handleSimulateReading}>
          <label>
            Device key
            <input value={deviceKey} onChange={(e) => setDeviceKey(e.target.value)} placeholder="Paste X-Device-Key value" required />
          </label>
          <label>
            Temperature (°C)
            <input type="number" step="0.1" value={simulateTemp} onChange={(e) => setSimulateTemp(e.target.value)} required />
          </label>
          <button type="submit" className="btn btn-secondary" disabled={simulating}>
            {simulating ? 'Sending…' : 'Send reading'}
          </button>
        </form>
      </div>

      <div className="card stack">
        <h2>Temperature history</h2>
        <TemperatureChart readings={readings} minTempC={device.minTempC} maxTempC={device.maxTempC} />
      </div>

      <div className="card stack">
        <h2>Recent readings</h2>
        {readings.length === 0 && <p className="muted">No readings for this device yet.</p>}
        <div className="table-wrap">
          <table>
            <thead>
              <tr>
                <th>Temperature</th>
                <th>Measured</th>
                <th>Received</th>
              </tr>
            </thead>
            <tbody>
              {readings.map((reading) => (
                <tr key={reading.id}>
                  <td>{reading.temperatureC}°C</td>
                  <td>{formatDateTime(reading.measuredAtUtc)}</td>
                  <td>{formatDateTime(reading.receivedAtUtc)}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </div>
    </section>
  );
}
