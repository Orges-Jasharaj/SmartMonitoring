import { useEffect, useState, type FormEvent } from 'react';
import { Link, useParams, useSearchParams } from 'react-router-dom';
import { api } from '../api/client';
import type { Device, DeviceCreated, Reading } from '../api/types';
import { StatCard } from '../components/StatCard';
import { TemperatureChart } from '../components/TemperatureChart';
import { useToast } from '../components/Toast';
import { useCompanyData } from '../hooks/useCompanyData';
import { useAuth } from '../auth/AuthContext';
import { canManageCompanyDevices, copyToClipboard, formatDateTime, getDeviceStatus } from '../utils/monitoring';

type Tab = 'overview' | 'devices' | 'readings' | 'alerts' | 'team';

const VALID_TABS: Tab[] = ['overview', 'devices', 'readings', 'alerts', 'team'];

function parseTab(value: string | null): Tab {
  if (value && VALID_TABS.includes(value as Tab)) {
    return value as Tab;
  }
  return 'overview';
}

export function CompanyPage() {
  const { companyId = '' } = useParams();
  const [searchParams] = useSearchParams();
  const { token, isAdmin, userId } = useAuth();
  const { pushToast } = useToast();
  const { company, devices, alerts, alertHistory, readings, members, users, loading, error, lastUpdated, refresh, stats } =
    useCompanyData(companyId, token);

  const canManageDevices = canManageCompanyDevices(isAdmin, userId, members);

  const [tab, setTab] = useState<Tab>(() => parseTab(searchParams.get('tab')));

  useEffect(() => {
    setTab(parseTab(searchParams.get('tab')));
  }, [searchParams]);

  const [showHistory, setShowHistory] = useState(false);
  const [readingDeviceFilter, setReadingDeviceFilter] = useState('');
  const [createdDevice, setCreatedDevice] = useState<DeviceCreated | null>(null);
  const [deviceForm, setDeviceForm] = useState({ name: '', zoneName: '', minTempC: '2', maxTempC: '8' });
  const [assignUserId, setAssignUserId] = useState('');
  const [assignRole, setAssignRole] = useState('CompanyAdmin');

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
      pushToast(response.message ?? 'Failed to create device', 'error');
      return;
    }

    setCreatedDevice(response.data);
    setDeviceForm({ name: '', zoneName: '', minTempC: '2', maxTempC: '8' });
    pushToast(`Device "${response.data.name}" created`, 'success');
    await refresh();
  }

  async function handleAssignUser(event: FormEvent) {
    event.preventDefault();
    if (!token || !companyId || !assignUserId) return;

    const response = await api.assignCompanyUser(token, companyId, assignUserId, assignRole);
    if (!response.success) {
      pushToast(response.message ?? 'Failed to assign user', 'error');
      return;
    }

    setAssignUserId('');
    pushToast('User assigned to company', 'success');
    await refresh();
  }

  async function handleCopyKey(key: string) {
    await copyToClipboard(key);
    pushToast('Device key copied to clipboard', 'success');
  }

  async function handleDeleteDevice(device: Device) {
    if (!token || !companyId) return;
    if (!window.confirm(`Delete device "${device.name}"? This also removes its readings and alerts.`)) {
      return;
    }

    const response = await api.deleteDevice(token, companyId, device.id);
    if (!response.success) {
      pushToast(response.message ?? 'Failed to delete device', 'error');
      return;
    }

    pushToast(`Device "${device.name}" deleted`, 'success');
    await refresh();
  }

  function deviceName(deviceId: string) {
    return devices.find((d) => d.id === deviceId)?.name ?? deviceId.slice(0, 8);
  }

  const filteredReadings = readingDeviceFilter
    ? readings.filter((r) => r.deviceId === readingDeviceFilter)
    : readings;

  const chartDevice = devices[0];
  const chartReadings = chartDevice
    ? readings.filter((r) => r.deviceId === chartDevice.id)
    : [];

  if (loading && !company) {
    return <p className="muted page-loading">Loading company…</p>;
  }

  return (
    <section className="stack">
      <div className="page-header">
        <div>
          <Link to="/" className="back-link">
            ← Companies
          </Link>
          <h1>{company?.name ?? 'Company'}</h1>
          <p className="muted">
            {lastUpdated ? `Updated ${lastUpdated.toLocaleTimeString()}` : 'Monitor devices, readings, and alerts.'}
          </p>
        </div>
        <div className="header-actions">
          <button type="button" className="btn btn-ghost" onClick={() => void refresh()}>
            Refresh
          </button>
        </div>
      </div>

      {error && <p className="error-banner">{error}</p>}

      <div className="stat-grid">
        <StatCard label="Devices" value={stats.deviceCount} />
        <StatCard label="Active alerts" value={stats.activeAlerts} tone={stats.activeAlerts > 0 ? 'danger' : 'ok'} />
        <StatCard label="Devices OK" value={stats.devicesOk} tone="ok" />
        <StatCard label="Team members" value={stats.memberCount} hint="Alert email recipients" />
      </div>

      <div className="tabs">
        {(['overview', 'devices', 'readings', 'alerts', 'team'] as Tab[]).map((item) => (
          <button key={item} type="button" className={tab === item ? 'tab active' : 'tab'} onClick={() => setTab(item)}>
            {item.charAt(0).toUpperCase() + item.slice(1)}
            {item === 'alerts' && stats.activeAlerts > 0 && <span className="tab-badge">{stats.activeAlerts}</span>}
          </button>
        ))}
      </div>

      {tab === 'overview' && (
        <div className="grid two-col">
          <div className="card stack">
            <h2>Temperature trend</h2>
            {chartDevice ? (
              <>
                <p className="muted small">Latest device: {chartDevice.name}</p>
                <TemperatureChart readings={chartReadings} minTempC={chartDevice.minTempC} maxTempC={chartDevice.maxTempC} />
              </>
            ) : (
              <p className="muted">Add a device to see temperature trends.</p>
            )}
          </div>
          <div className="stack">
            <div className="card stack">
              <h2>Device status</h2>
              {devices.length === 0 && <p className="muted">No devices yet.</p>}
              <ul className="status-list">
                {devices.map((device) => {
                  const status = getDeviceStatus(device, readings);
                  return (
                    <li key={device.id}>
                      <Link
                        to={`/companies/${companyId}/devices/${device.id}`}
                        className={`status-row${status.tone === 'danger' ? ' status-row-alert' : ''}`}
                      >
                        <div>
                          <strong>{device.name}</strong>
                          <span className="muted small">{device.zoneName}</span>
                        </div>
                        <div className="status-row-meta">
                          {status.latestTemp !== undefined && <span>{status.latestTemp}°C</span>}
                          <span className={`pill pill-${status.tone}`}>{status.label}</span>
                        </div>
                      </Link>
                    </li>
                  );
                })}
              </ul>
            </div>
            <div className="card stack">
              <h2>Recent alerts</h2>
              {alerts.length === 0 && <p className="muted">No active alerts.</p>}
              <ul className="alert-list compact">
                {alerts.slice(0, 5).map((alert) => (
                  <li key={alert.id} className="alert-item alert-item-active">
                    <strong>{deviceName(alert.deviceId)}</strong>
                    <p className="small">{alert.message}</p>
                  </li>
                ))}
              </ul>
            </div>
          </div>
        </div>
      )}

      {tab === 'devices' && (
        <div className="grid two-col">
          <div className="card stack">
            <h2>Devices</h2>
            <div className="device-grid">
              {devices.map((device) => (
                <DeviceCard
                  key={device.id}
                  device={device}
                  companyId={companyId}
                  readings={readings}
                  canManageDevices={canManageDevices}
                  onDelete={() => void handleDeleteDevice(device)}
                />
              ))}
            </div>
          </div>
          <div className="stack">
            <form className="card stack" onSubmit={handleCreateDevice}>
              <h2>Add device</h2>
              <label>
                Name
                <input value={deviceForm.name} onChange={(e) => setDeviceForm((f) => ({ ...f, name: e.target.value }))} required />
              </label>
              <label>
                Zone
                <input value={deviceForm.zoneName} onChange={(e) => setDeviceForm((f) => ({ ...f, zoneName: e.target.value }))} required />
              </label>
              <div className="inline-fields">
                <label>
                  Min °C
                  <input type="number" step="0.1" value={deviceForm.minTempC} onChange={(e) => setDeviceForm((f) => ({ ...f, minTempC: e.target.value }))} required />
                </label>
                <label>
                  Max °C
                  <input type="number" step="0.1" value={deviceForm.maxTempC} onChange={(e) => setDeviceForm((f) => ({ ...f, maxTempC: e.target.value }))} required />
                </label>
              </div>
              <button type="submit" className="btn btn-primary">Create device</button>
            </form>
            {createdDevice && (
              <div className="card stack highlight">
                <h3>Device key — save this once</h3>
                <code className="device-key">{createdDevice.deviceKey}</code>
                <div className="inline-actions">
                  <button type="button" className="btn btn-secondary" onClick={() => void handleCopyKey(createdDevice.deviceKey)}>
                    Copy key
                  </button>
                  <button type="button" className="btn btn-ghost" onClick={() => setCreatedDevice(null)}>Dismiss</button>
                </div>
              </div>
            )}
          </div>
        </div>
      )}

      {tab === 'readings' && (
        <div className="card stack">
          <div className="panel-header">
            <h2>Recent readings</h2>
            <select value={readingDeviceFilter} onChange={(e) => setReadingDeviceFilter(e.target.value)}>
              <option value="">All devices</option>
              {devices.map((d) => (
                <option key={d.id} value={d.id}>{d.name}</option>
              ))}
            </select>
          </div>
          {filteredReadings.length === 0 && <p className="muted">No readings yet.</p>}
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
                {filteredReadings.map((reading) => (
                  <tr key={reading.id}>
                    <td>{deviceName(reading.deviceId)}</td>
                    <td>{reading.temperatureC}°C</td>
                    <td>{formatDateTime(reading.measuredAtUtc)}</td>
                    <td>{formatDateTime(reading.receivedAtUtc)}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </div>
      )}

      {tab === 'alerts' && (
        <div className="card stack">
          <div className="panel-header">
            <h2>{showHistory ? 'Alert history' : 'Active alerts'}</h2>
            <button type="button" className="btn btn-ghost" onClick={() => setShowHistory((v) => !v)}>
              {showHistory ? 'Show active only' : 'Show history'}
            </button>
          </div>
          {(showHistory ? alertHistory : alerts).length === 0 && <p className="muted">No alerts to show.</p>}
          <ul className="alert-list">
            {(showHistory ? alertHistory : alerts).map((alert) => (
              <li key={alert.id} className={`alert-item ${alert.isActive ? 'alert-item-active' : 'resolved'}`}>
                <div>
                  <strong>{deviceName(alert.deviceId)}</strong>
                  <span className={`pill ${alert.isActive ? 'pill-danger' : 'pill-muted'}`}>{alert.alertType}</span>
                </div>
                <p>{alert.message}</p>
                <p className="muted small">{formatDateTime(alert.triggeredAtUtc)}</p>
              </li>
            ))}
          </ul>
        </div>
      )}

      {tab === 'team' && (
        <div className="grid two-col">
          <div className="card stack">
            <h2>Team members</h2>
            <p className="muted small">Users assigned here receive alert emails for this company.</p>
            {members.length === 0 && <p className="muted">No users assigned yet.</p>}
            <ul className="team-list">
              {members.map((member) => {
                const user = users.find((u) => u.id === member.userId);
                return (
                  <li key={member.id} className="team-row">
                    <div>
                      <strong>{user?.userName ?? member.userId}</strong>
                      <span className="muted small">{user?.email ?? '—'}</span>
                    </div>
                    <span className="pill pill-muted">{member.role}</span>
                  </li>
                );
              })}
            </ul>
          </div>
          <form className="card stack" onSubmit={handleAssignUser}>
            <h2>Assign user</h2>
            <label>
              User
              <select value={assignUserId} onChange={(e) => setAssignUserId(e.target.value)} required>
                <option value="">Select user…</option>
                {users.map((user) => (
                  <option key={user.id} value={user.id}>{user.userName} ({user.email})</option>
                ))}
              </select>
            </label>
            <label>
              Role
              <select value={assignRole} onChange={(e) => setAssignRole(e.target.value)}>
                <option value="CompanyAdmin">CompanyAdmin</option>
                <option value="CompanyViewer">CompanyViewer</option>
              </select>
            </label>
            <button type="submit" className="btn btn-secondary" disabled={!assignUserId}>Assign user</button>
          </form>
        </div>
      )}
    </section>
  );
}

function DeviceCard({
  device,
  companyId,
  readings,
  canManageDevices,
  onDelete,
}: {
  device: Device;
  companyId: string;
  readings: Reading[];
  canManageDevices?: boolean;
  onDelete?: () => void;
}) {
  const status = getDeviceStatus(device, readings);
  const hasAlert = status.tone === 'danger';
  return (
    <div className={`device-card${hasAlert ? ' device-card-alert' : ''}`}>
      <Link to={`/companies/${companyId}/devices/${device.id}`} className="device-card-body link-card">
        <div className="device-card-header">
          <strong>{device.name}</strong>
          <span className={`pill pill-${status.tone}`}>{status.label}</span>
        </div>
        <p className="muted">{device.zoneName}</p>
        <p>Range: {device.minTempC}°C – {device.maxTempC}°C</p>
        {status.latestTemp !== undefined && <p className="temp-reading">{status.latestTemp}°C</p>}
        <p className="muted small">Last: {formatDateTime(device.lastReadingAtUtc)}</p>
      </Link>
      {canManageDevices && onDelete && (
        <button type="button" className="btn btn-ghost btn-sm device-delete-btn" onClick={onDelete}>
          Delete
        </button>
      )}
    </div>
  );
}
