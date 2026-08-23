import { useEffect, useState, type FormEvent } from 'react';
import { Link, useParams } from 'react-router-dom';
import { api } from '../api/client';
import type { Alert, Company, Device, DeviceCreated, Reading, User } from '../api/types';
import { useAuth } from '../auth/AuthContext';

type Tab = 'devices' | 'alerts' | 'readings';

export function CompanyPage() {
  const { companyId = '' } = useParams();
  const { token } = useAuth();
  const [tab, setTab] = useState<Tab>('devices');
  const [company, setCompany] = useState<Company | null>(null);
  const [devices, setDevices] = useState<Device[]>([]);
  const [alerts, setAlerts] = useState<Alert[]>([]);
  const [readings, setReadings] = useState<Reading[]>([]);
  const [users, setUsers] = useState<User[]>([]);
  const [error, setError] = useState<string | null>(null);
  const [createdDevice, setCreatedDevice] = useState<DeviceCreated | null>(null);

  const [deviceForm, setDeviceForm] = useState({
    name: '',
    zoneName: '',
    minTempC: '2',
    maxTempC: '8',
  });

  const [assignUserId, setAssignUserId] = useState('');

  async function refresh() {
    if (!token || !companyId) return;
    setError(null);

    const [companyRes, devicesRes, alertsRes, readingsRes, usersRes] = await Promise.all([
      api.getCompany(token, companyId),
      api.getDevices(token, companyId),
      api.getAlerts(token, companyId, true),
      api.getReadings(token, companyId, undefined, 30),
      api.getUsers(token),
    ]);

    if (companyRes.success && companyRes.data) setCompany(companyRes.data);
    if (devicesRes.success && devicesRes.data) setDevices(devicesRes.data);
    if (alertsRes.success && alertsRes.data) setAlerts(alertsRes.data);
    if (readingsRes.success && readingsRes.data) setReadings(readingsRes.data);
    if (usersRes.success && usersRes.data) setUsers(usersRes.data);

    if (!companyRes.success) {
      setError(companyRes.message ?? 'Failed to load company');
    }
  }

  useEffect(() => {
    void refresh();
  }, [token, companyId]);

  async function handleCreateDevice(event: FormEvent) {
    event.preventDefault();
    if (!token || !companyId) return;

    const response = await api.createDevice(token, companyId, {
      name: deviceForm.name.trim(),
      zoneName: deviceForm.zoneName.trim(),
      minTempC: Number(deviceForm.minTempC),
      maxTempC: Number(deviceForm.maxTempC),
    });

    if (!response.success || !response.data) {
      setError(response.message ?? 'Failed to create device');
      return;
    }

    setCreatedDevice(response.data);
    setDeviceForm({ name: '', zoneName: '', minTempC: '2', maxTempC: '8' });
    await refresh();
  }

  async function handleAssignUser(event: FormEvent) {
    event.preventDefault();
    if (!token || !companyId || !assignUserId) return;

    const response = await api.assignCompanyUser(token, companyId, assignUserId, 'CompanyAdmin');
    if (!response.success) {
      setError(response.message ?? 'Failed to assign user');
      return;
    }

    setAssignUserId('');
    setError(null);
  }

  function deviceName(deviceId: string) {
    return devices.find((d) => d.id === deviceId)?.name ?? deviceId.slice(0, 8);
  }

  function deviceStatus(device: Device) {
    const latest = readings.find((r) => r.deviceId === device.id);
    if (!latest) return { label: 'No data', tone: 'muted' as const };
    if (latest.temperatureC < device.minTempC || latest.temperatureC > device.maxTempC) {
      return { label: 'Out of range', tone: 'danger' as const };
    }
    return { label: 'OK', tone: 'ok' as const };
  }

  return (
    <section className="stack">
      <div className="page-header">
        <div>
          <Link to="/" className="back-link">
            ← Companies
          </Link>
          <h1>{company?.name ?? 'Company'}</h1>
          <p>Monitor devices, temperature readings, and active alerts.</p>
        </div>
        <button type="button" className="btn btn-ghost" onClick={() => void refresh()}>
          Refresh
        </button>
      </div>

      {error && <p className="error-banner">{error}</p>}

      <div className="tabs">
        {(['devices', 'alerts', 'readings'] as Tab[]).map((item) => (
          <button
            key={item}
            type="button"
            className={tab === item ? 'tab active' : 'tab'}
            onClick={() => setTab(item)}
          >
            {item.charAt(0).toUpperCase() + item.slice(1)}
          </button>
        ))}
      </div>

      {tab === 'devices' && (
        <div className="grid two-col">
          <div className="card stack">
            <h2>Devices</h2>
            {devices.length === 0 && <p className="muted">No devices registered yet.</p>}
            <div className="device-grid">
              {devices.map((device) => {
                const status = deviceStatus(device);
                return (
                  <article key={device.id} className="device-card">
                    <div className="device-card-header">
                      <strong>{device.name}</strong>
                      <span className={`pill pill-${status.tone}`}>{status.label}</span>
                    </div>
                    <p className="muted">{device.zoneName}</p>
                    <p>
                      Range: {device.minTempC}°C – {device.maxTempC}°C
                    </p>
                    <p className="muted small">
                      Last reading:{' '}
                      {device.lastReadingAtUtc
                        ? new Date(device.lastReadingAtUtc).toLocaleString()
                        : 'Never'}
                    </p>
                  </article>
                );
              })}
            </div>
          </div>

          <div className="stack">
            <form className="card stack" onSubmit={handleCreateDevice}>
              <h2>Add device</h2>
              <label>
                Name
                <input
                  value={deviceForm.name}
                  onChange={(e) => setDeviceForm((f) => ({ ...f, name: e.target.value }))}
                  required
                />
              </label>
              <label>
                Zone
                <input
                  value={deviceForm.zoneName}
                  onChange={(e) => setDeviceForm((f) => ({ ...f, zoneName: e.target.value }))}
                  required
                />
              </label>
              <div className="inline-fields">
                <label>
                  Min °C
                  <input
                    type="number"
                    step="0.1"
                    value={deviceForm.minTempC}
                    onChange={(e) => setDeviceForm((f) => ({ ...f, minTempC: e.target.value }))}
                    required
                  />
                </label>
                <label>
                  Max °C
                  <input
                    type="number"
                    step="0.1"
                    value={deviceForm.maxTempC}
                    onChange={(e) => setDeviceForm((f) => ({ ...f, maxTempC: e.target.value }))}
                    required
                  />
                </label>
              </div>
              <button type="submit" className="btn btn-primary">
                Create device
              </button>
            </form>

            {createdDevice && (
              <div className="card stack highlight">
                <h3>Device key (save this once)</h3>
                <code className="device-key">{createdDevice.deviceKey}</code>
                <p className="muted small">
                  Use header <strong>X-Device-Key</strong> when ingesting readings from the IoT device or Postman.
                </p>
                <button type="button" className="btn btn-ghost" onClick={() => setCreatedDevice(null)}>
                  Dismiss
                </button>
              </div>
            )}

            <form className="card stack" onSubmit={handleAssignUser}>
              <h2>Alert recipients</h2>
              <p className="muted small">
                Assign a user to this company so alert emails are sent when temperature goes out of range.
              </p>
              <label>
                User
                <select value={assignUserId} onChange={(e) => setAssignUserId(e.target.value)} required>
                  <option value="">Select user…</option>
                  {users.map((user) => (
                    <option key={user.id} value={user.id}>
                      {user.userName} ({user.email})
                    </option>
                  ))}
                </select>
              </label>
              <button type="submit" className="btn btn-secondary" disabled={!assignUserId}>
                Assign as CompanyAdmin
              </button>
            </form>
          </div>
        </div>
      )}

      {tab === 'alerts' && (
        <div className="card stack">
          <h2>Active alerts</h2>
          {alerts.length === 0 && <p className="muted">No active alerts.</p>}
          <ul className="alert-list">
            {alerts.map((alert) => (
              <li key={alert.id} className="alert-item">
                <div>
                  <strong>{deviceName(alert.deviceId)}</strong>
                  <span className="pill pill-danger">{alert.alertType}</span>
                </div>
                <p>{alert.message}</p>
                <p className="muted small">{new Date(alert.triggeredAtUtc).toLocaleString()}</p>
              </li>
            ))}
          </ul>
        </div>
      )}

      {tab === 'readings' && (
        <div className="card stack">
          <h2>Recent readings</h2>
          {readings.length === 0 && <p className="muted">No readings yet.</p>}
          <div className="table-wrap">
            <table>
              <thead>
                <tr>
                  <th>Device</th>
                  <th>Temperature</th>
                  <th>Measured</th>
                  <th>Received</th>
                </tr>
              </thead>
              <tbody>
                {readings.map((reading) => (
                  <tr key={reading.id}>
                    <td>{deviceName(reading.deviceId)}</td>
                    <td>{reading.temperatureC}°C</td>
                    <td>{new Date(reading.measuredAtUtc).toLocaleString()}</td>
                    <td>{new Date(reading.receivedAtUtc).toLocaleString()}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </div>
      )}
    </section>
  );
}
