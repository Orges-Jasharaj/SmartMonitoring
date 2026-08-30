import { useCallback, useEffect, useState, type FormEvent } from 'react';
import { Link, useNavigate, useParams } from 'react-router-dom';
import { api } from '../api/client';
import type { CompanyUser, Device, Reading } from '../api/types';
import { TemperatureChart } from '../components/TemperatureChart';
import { StatCard } from '../components/StatCard';
import { useToast } from '../components/Toast';
import { useAuth } from '../auth/AuthContext';
import { useMonitoringHub } from '../hooks/useMonitoringHub';
import { useMonitoringClock } from '../hooks/useMonitoringClock';
import { prependReading } from '../realtime/monitoringHub';
import { canManageCompanyDevices, formatDateTime, getDeviceStatus } from '../utils/monitoring';

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
  const [simulateTemp, setSimulateTemp] = useState('6');
  const [simulating, setSimulating] = useState(false);
  const [savingSettings, setSavingSettings] = useState(false);
  const [settingsForm, setSettingsForm] = useState({
    name: '',
    zoneName: '',
    minTempC: '',
    maxTempC: '',
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
        minTempC: String(deviceRes.data.minTempC),
        maxTempC: String(deviceRes.data.maxTempC),
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
        current && current.id === reading.deviceId
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
    const response = await api.ingestReading(deviceKey.trim(), Number(simulateTemp));
    setSimulating(false);

    if (!response.success) {
      pushToast(response.message ?? 'Ingest failed', 'error');
      return;
    }

    pushToast(`Reading ${simulateTemp}°C ingested`, 'success');
  }

  async function handleUpdateSettings(event: FormEvent) {
    event.preventDefault();
    if (!token || !companyId || !deviceId) return;

    setSavingSettings(true);
    const response = await api.updateDevice(token, companyId, deviceId, {
      name: settingsForm.name.trim(),
      zoneName: settingsForm.zoneName.trim(),
      minTempC: Number(settingsForm.minTempC),
      maxTempC: Number(settingsForm.maxTempC),
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

  const status = getDeviceStatus(device, readings, now);

  return (
    <section className="stack">
      <div className="page-header">
        <div>
          <Link to={`/companies/${companyId}`} className="back-link">← Back to company</Link>
          <h1>{device.name}</h1>
          <p className="muted">{device.zoneName} · Safe range {device.minTempC}°C – {device.maxTempC}°C</p>
        </div>
        <div className="header-actions">
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
          hint={status.tone === 'warning' ? 'No reading for 30+ minutes' : undefined}
        />
        <StatCard label="Latest temp" value={status.latestTemp !== undefined ? `${status.latestTemp}°C` : '—'} />
        <StatCard label="Readings loaded" value={readings.length} />
        <StatCard label="Last reading" value={formatDateTime(device.lastReadingAtUtc)} />
      </div>

      {canManageDevices && (
        <form id="device-settings" className="card stack" onSubmit={handleUpdateSettings}>
          <h2>Device settings</h2>
          <p className="muted small">Update name, zone, and safe temperature range.</p>
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
          <div className="inline-fields">
            <label>
              Min °C
              <input
                type="number"
                step="0.1"
                value={settingsForm.minTempC}
                onChange={(e) => setSettingsForm((f) => ({ ...f, minTempC: e.target.value }))}
                required
              />
            </label>
            <label>
              Max °C
              <input
                type="number"
                step="0.1"
                value={settingsForm.maxTempC}
                onChange={(e) => setSettingsForm((f) => ({ ...f, maxTempC: e.target.value }))}
                required
              />
            </label>
          </div>
          <button type="submit" className="btn btn-primary" disabled={savingSettings}>
            {savingSettings ? 'Saving…' : 'Save settings'}
          </button>
        </form>
      )}

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
