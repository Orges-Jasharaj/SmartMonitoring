import { useEffect, useState, type FormEvent } from 'react';
import { Link, useNavigate, useParams, useSearchParams } from 'react-router-dom';
import { api } from '../api/client';
import type { Alert, Device, DeviceCreated, Reading, User } from '../api/types';
import { StatCard } from '../components/StatCard';
import { AlertAcknowledgeButton } from '../components/AlertAcknowledgeButton';
import { EnvironmentalCharts } from '../components/EnvironmentalCharts';
import { Pagination } from '../components/Pagination';
import { useToast } from '../components/Toast';
import { useCompanyData } from '../hooks/useCompanyData';
import { useAuth } from '../auth/AuthContext';
import { DeviceThresholdFields } from '../components/DeviceThresholdFields';
import { DEVICE_THRESHOLD_DEFAULTS, parseDeviceThresholdPayload } from '../constants/deviceDefaults';
import {
  canManageCompanyDevices,
  copyToClipboard,
  deviceCardClass,
  deviceStatusRowClass,
  formatDateTime,
  getDeviceStatusFromSnapshot,
} from '../utils/monitoring';
import { useMonitoringClock } from '../hooks/useMonitoringClock';

type Tab = 'overview' | 'devices' | 'readings' | 'alerts' | 'team';

const VALID_TABS: Tab[] = ['overview', 'devices', 'readings', 'alerts', 'team'];
const PAGE_SIZE = 25;

function parseTab(value: string | null): Tab {
  if (value && VALID_TABS.includes(value as Tab)) {
    return value as Tab;
  }
  return 'overview';
}

export function CompanyPage() {
  const { companyId = '' } = useParams();
  const navigate = useNavigate();
  const [searchParams] = useSearchParams();
  const { token, isAdmin, userId } = useAuth();
  const { pushToast } = useToast();
  const { company, devices, alerts, members, loading, error, lastUpdated, refresh, stats } =
    useCompanyData(companyId, token);

  const canManageDevices = canManageCompanyDevices(isAdmin, userId, members);
  const now = useMonitoringClock();

  const [tab, setTab] = useState<Tab>(() => parseTab(searchParams.get('tab')));

  useEffect(() => {
    setTab(parseTab(searchParams.get('tab')));
  }, [searchParams]);

  const [showHistory, setShowHistory] = useState(false);
  const [readingDeviceFilter, setReadingDeviceFilter] = useState('');
  const [readings, setReadings] = useState<Reading[]>([]);
  const [readingsPage, setReadingsPage] = useState(1);
  const [readingsTotalCount, setReadingsTotalCount] = useState(0);
  const [readingsLoading, setReadingsLoading] = useState(false);
  const [overviewChartReadings, setOverviewChartReadings] = useState<Reading[]>([]);
  const [tabAlerts, setTabAlerts] = useState<Alert[]>([]);
  const [alertsPage, setAlertsPage] = useState(1);
  const [alertsTotalCount, setAlertsTotalCount] = useState(0);
  const [alertsLoading, setAlertsLoading] = useState(false);
  const [tabDevices, setTabDevices] = useState<Device[]>([]);
  const [devicesPage, setDevicesPage] = useState(1);
  const [devicesTotalCount, setDevicesTotalCount] = useState(0);
  const [devicesLoading, setDevicesLoading] = useState(false);
  const [assignUsers, setAssignUsers] = useState<User[]>([]);
  const [createdDevice, setCreatedDevice] = useState<DeviceCreated | null>(null);
  const [deviceForm, setDeviceForm] = useState({ name: '', zoneName: '', ...DEVICE_THRESHOLD_DEFAULTS });
  const [assignUserId, setAssignUserId] = useState('');
  const [assignRole, setAssignRole] = useState('CompanyAdmin');

  async function handleCreateDevice(event: FormEvent) {
    event.preventDefault();
    if (!token || !companyId) return;

    const response = await api.createDevice(token, companyId, {
      name: deviceForm.name.trim(),
      zoneName: deviceForm.zoneName.trim(),
      ...parseDeviceThresholdPayload(deviceForm),
    });

    if (!response.success || !response.data) {
      pushToast(response.message ?? 'Failed to create device', 'error');
      return;
    }

    setCreatedDevice(response.data);
    setDeviceForm({ name: '', zoneName: '', ...DEVICE_THRESHOLD_DEFAULTS });
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

  async function handleDeleteCompany() {
    if (!token || !companyId || !company) return;
    if (
      !window.confirm(
        `Delete company "${company.name}"? This permanently removes its devices, readings, alerts, and team assignments.`,
      )
    ) {
      return;
    }

    const response = await api.deleteCompany(token, companyId);
    if (!response.success) {
      pushToast(response.message ?? 'Failed to delete company', 'error');
      return;
    }

    pushToast(`Company "${company.name}" deleted`, 'success');
    navigate('/');
  }

  function deviceName(deviceId: string) {
    return devices.find((d) => d.id === deviceId)?.name ?? deviceId.slice(0, 8);
  }

  useEffect(() => {
    if (!token || !companyId || tab !== 'readings') return;
    setReadingsLoading(true);
    void api
      .getReadings(token, companyId, {
        deviceId: readingDeviceFilter || undefined,
        page: readingsPage,
        pageSize: PAGE_SIZE,
      })
      .then((response) => {
        setReadings(response.data?.items ?? []);
        setReadingsTotalCount(response.data?.totalCount ?? 0);
        setReadingsPage(response.data?.page ?? readingsPage);
        setReadingsLoading(false);
      });
  }, [token, companyId, tab, readingsPage, readingDeviceFilter]);

  useEffect(() => {
    if (!token || !companyId || tab !== 'alerts') return;
    setAlertsLoading(true);
    void api
      .getAlerts(token, companyId, {
        activeOnly: !showHistory,
        page: alertsPage,
        pageSize: PAGE_SIZE,
      })
      .then((response) => {
        setTabAlerts(response.data?.items ?? []);
        setAlertsTotalCount(response.data?.totalCount ?? 0);
        setAlertsPage(response.data?.page ?? alertsPage);
        setAlertsLoading(false);
      });
  }, [token, companyId, tab, alertsPage, showHistory]);

  useEffect(() => {
    if (!token || !companyId || tab !== 'devices') return;
    setDevicesLoading(true);
    void api
      .getDevices(token, companyId, { page: devicesPage, pageSize: PAGE_SIZE })
      .then((response) => {
        setTabDevices(response.data?.items ?? []);
        setDevicesTotalCount(response.data?.totalCount ?? 0);
        setDevicesPage(response.data?.page ?? devicesPage);
        setDevicesLoading(false);
      });
  }, [token, companyId, tab, devicesPage]);

  useEffect(() => {
    if (!token || tab !== 'team') return;
    void api.getUsers(token, { page: 1, pageSize: 200 }).then((response) => {
      setAssignUsers(response.data?.items ?? []);
    });
  }, [token, tab]);

  useEffect(() => {
    if (!token || !companyId || tab !== 'overview' || !devices[0]) {
      setOverviewChartReadings([]);
      return;
    }

    void api
      .getReadings(token, companyId, { deviceId: devices[0].id, page: 1, pageSize: 100 })
      .then((response) => setOverviewChartReadings(response.data?.items ?? []));
  }, [token, companyId, tab, devices]);

  useEffect(() => {
    setReadingsPage(1);
  }, [readingDeviceFilter]);

  useEffect(() => {
    setAlertsPage(1);
  }, [showHistory]);

  const chartDevice = devices[0];

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
          {isAdmin && company && (
            <button type="button" className="btn btn-ghost company-delete-btn" onClick={() => void handleDeleteCompany()}>
              Delete company
            </button>
          )}
        </div>
      </div>

      {error && <p className="error-banner">{error}</p>}

      <div className="stat-grid">
        <StatCard label="Devices" value={stats.deviceCount} />
        <StatCard label="Active alerts" value={stats.activeAlerts} tone={stats.activeAlerts > 0 ? 'danger' : 'ok'} />
        <StatCard label="Devices OK" value={stats.devicesOk} tone="ok" />
        <StatCard
          label="Offline"
          value={stats.devicesOffline}
          tone={stats.devicesOffline > 0 ? 'warning' : 'default'}
          hint="No reading for 30+ min"
        />
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
            <h2>Environmental trends</h2>
            {chartDevice ? (
              <>
                <p className="muted small">Latest device: {chartDevice.name}</p>
                <EnvironmentalCharts device={chartDevice} readings={overviewChartReadings} compact />
              </>
            ) : (
              <p className="muted">Add a device to see environmental trends.</p>
            )}
          </div>
          <div className="stack">
            <div className="card stack">
              <h2>Device status</h2>
              {devices.length === 0 && <p className="muted">No devices yet.</p>}
              <ul className="status-list">
                {devices.map((device) => {
                  const deviceAlerts = alerts.filter((alert) => alert.deviceId === device.id);
                  const status = getDeviceStatusFromSnapshot(device, deviceAlerts, now);
                  return (
                    <li key={device.id}>
                      <Link
                        to={`/companies/${companyId}/devices/${device.id}`}
                        className={`status-row${deviceStatusRowClass(status.tone)}`}
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
                    <div className="alert-item-row">
                      <div className="alert-item-body">
                        <strong>{deviceName(alert.deviceId)}</strong>
                        <p className="small">{alert.message}</p>
                      </div>
                      <AlertAcknowledgeButton
                        token={token}
                        companyId={companyId}
                        alert={alert}
                        onAcknowledged={() => {
                          pushToast('Alert acknowledged', 'success');
                          void refresh();
                        }}
                        onError={(message) => pushToast(message, 'error')}
                      />
                    </div>
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
            {devicesLoading && tabDevices.length === 0 && <p className="muted">Loading…</p>}
            <div className="device-grid">
              {tabDevices.map((device) => (
                <DeviceCard
                  key={device.id}
                  device={device}
                  companyId={companyId}
                  alerts={alerts}
                  canManageDevices={canManageDevices}
                  onDelete={() => void handleDeleteDevice(device)}
                />
              ))}
            </div>
            <Pagination
              page={devicesPage}
              pageSize={PAGE_SIZE}
              totalCount={devicesTotalCount}
              loading={devicesLoading}
              onPageChange={setDevicesPage}
            />
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
              <DeviceThresholdFields
                form={deviceForm}
                onChange={(next) => setDeviceForm((current) => ({ ...current, ...next }))}
              />
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
          {readingsLoading && readings.length === 0 && <p className="muted">Loading…</p>}
          {!readingsLoading && readings.length === 0 && <p className="muted">No readings yet.</p>}
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
                    <td>{formatDateTime(reading.measuredAtUtc)}</td>
                    <td>{formatDateTime(reading.receivedAtUtc)}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
          <Pagination
            page={readingsPage}
            pageSize={PAGE_SIZE}
            totalCount={readingsTotalCount}
            loading={readingsLoading}
            onPageChange={setReadingsPage}
          />
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
          {alertsLoading && tabAlerts.length === 0 && <p className="muted">Loading…</p>}
          {!alertsLoading && tabAlerts.length === 0 && <p className="muted">No alerts to show.</p>}
          <ul className="alert-list">
            {tabAlerts.map((alert) => (
              <li key={alert.id} className={`alert-item ${alert.isActive ? 'alert-item-active' : 'resolved'}`}>
                <div className="alert-item-row">
                  <div className="alert-item-body">
                    <div>
                      <strong>{deviceName(alert.deviceId)}</strong>
                      <span className={`pill ${alert.isActive ? 'pill-danger' : 'pill-muted'}`}>{alert.alertType}</span>
                    </div>
                    <p>{alert.message}</p>
                    <p className="muted small">{formatDateTime(alert.triggeredAtUtc)}</p>
                    {!alert.isActive && alert.resolvedAtUtc && (
                      <p className="muted small">Resolved {formatDateTime(alert.resolvedAtUtc)}</p>
                    )}
                  </div>
                  <AlertAcknowledgeButton
                    token={token}
                    companyId={companyId}
                    alert={alert}
                    onAcknowledged={() => {
                      pushToast('Alert acknowledged', 'success');
                      void refresh();
                      setAlertsPage(1);
                    }}
                    onError={(message) => pushToast(message, 'error')}
                  />
                </div>
              </li>
            ))}
          </ul>
          <Pagination
            page={alertsPage}
            pageSize={PAGE_SIZE}
            totalCount={alertsTotalCount}
            loading={alertsLoading}
            onPageChange={setAlertsPage}
          />
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
                const user = assignUsers.find((u) => u.id === member.userId);
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
                {assignUsers.map((user) => (
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
  alerts,
  canManageDevices,
  onDelete,
}: {
  device: Device;
  companyId: string;
  alerts: Alert[];
  canManageDevices?: boolean;
  onDelete?: () => void;
}) {
  const now = useMonitoringClock();
  const deviceAlerts = alerts.filter((alert) => alert.deviceId === device.id);
  const status = getDeviceStatusFromSnapshot(device, deviceAlerts, now);
  const lastReadingAt = status.lastReadingAt;

  return (
    <div className={`device-card${deviceCardClass(status.tone)}`}>
      <Link to={`/companies/${companyId}/devices/${device.id}`} className="device-card-body link-card">
        <div className="device-card-header">
          <strong>{device.name}</strong>
          <span className={`pill pill-${status.tone}`}>{status.label}</span>
        </div>
        <p className="muted">{device.zoneName}</p>
        <p>Range: {device.minTempC}°C – {device.maxTempC}°C</p>
        {status.latestTemp !== undefined && <p className="temp-reading">{status.latestTemp}°C</p>}
        <p className="muted small">Last: {formatDateTime(lastReadingAt ?? device.lastReadingAtUtc)}</p>
      </Link>
      {canManageDevices && onDelete && (
        <div className="device-card-actions">
          <Link to={`/companies/${companyId}/devices/${device.id}#device-key`} className="btn btn-secondary btn-sm">
            Device key
          </Link>
          <Link to={`/companies/${companyId}/devices/${device.id}#device-settings`} className="btn btn-ghost btn-sm">
            Edit
          </Link>
          <button type="button" className="btn btn-ghost btn-sm device-delete-btn" onClick={onDelete}>
            Delete
          </button>
        </div>
      )}
    </div>
  );
}
