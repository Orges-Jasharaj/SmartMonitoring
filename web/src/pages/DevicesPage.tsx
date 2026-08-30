import { useMemo, useState, type FormEvent } from 'react';
import { Link } from 'react-router-dom';
import { api } from '../api/client';
import type { Device, DeviceCreated } from '../api/types';
import { StatCard } from '../components/StatCard';
import { useToast } from '../components/Toast';
import { useAuth } from '../auth/AuthContext';
import { useGlobalMonitoring } from '../hooks/useGlobalMonitoring';
import { canManageCompanyDevices, copyToClipboard, deviceStatusRowClass, formatDateTime } from '../utils/monitoring';

export function DevicesPage() {
  const { token, isAdmin, userId } = useAuth();
  const { pushToast } = useToast();
  const { devices, loading, error, refresh, totals, summaries, membersByCompanyId } = useGlobalMonitoring(token);

  const accessibleCompanies = useMemo(() => summaries.map((summary) => summary.company), [summaries]);

  const canManageAny = accessibleCompanies.length > 0;

  const [deviceForm, setDeviceForm] = useState({
    companyId: '',
    name: '',
    zoneName: '',
    minTempC: '2',
    maxTempC: '8',
  });
  const [creating, setCreating] = useState(false);
  const [createdDevice, setCreatedDevice] = useState<DeviceCreated | null>(null);
  const [deletingId, setDeletingId] = useState<string | null>(null);
  const [showCreateForm, setShowCreateForm] = useState(false);

  const selectedCompanyId = deviceForm.companyId || accessibleCompanies[0]?.id || '';

  function canManageCompany(companyId: string) {
    return canManageCompanyDevices(isAdmin, userId, membersByCompanyId[companyId] ?? []);
  }

  async function handleCreateDevice(event: FormEvent) {
    event.preventDefault();
    if (!token || !selectedCompanyId) return;

    setCreating(true);
    const response = await api.createDevice(token, selectedCompanyId, {
      name: deviceForm.name.trim(),
      zoneName: deviceForm.zoneName.trim(),
      minTempC: Number(deviceForm.minTempC),
      maxTempC: Number(deviceForm.maxTempC),
    });
    setCreating(false);

    if (!response.success || !response.data) {
      pushToast(response.message ?? 'Failed to create device', 'error');
      return;
    }

    setCreatedDevice(response.data);
    setDeviceForm({
      companyId: selectedCompanyId,
      name: '',
      zoneName: '',
      minTempC: '2',
      maxTempC: '8',
    });
    pushToast(`Device "${response.data.name}" created`, 'success');
    await refresh();
  }

  async function handleCopyKey(key: string) {
    await copyToClipboard(key);
    pushToast('Device key copied to clipboard', 'success');
  }

  async function handleDeleteDevice(device: Device, companyId: string) {
    if (!token) return;
    if (!window.confirm(`Delete device "${device.name}"? This also removes its readings and alerts.`)) {
      return;
    }

    setDeletingId(device.id);
    const response = await api.deleteDevice(token, companyId, device.id);
    setDeletingId(null);

    if (!response.success) {
      pushToast(response.message ?? 'Failed to delete device', 'error');
      return;
    }

    pushToast(`Device "${device.name}" deleted`, 'success');
    await refresh();
  }

  const devicesOk = devices.filter((item) => item.status.tone === 'ok').length;
  const devicesAlerting = devices.filter((item) => item.status.tone === 'danger').length;
  const devicesOffline = devices.filter((item) => item.status.tone === 'warning').length;

  return (
    <section className="stack">
      <div className="page-header">
        <div>
          <h1>Devices</h1>
          <p>All monitored devices across your companies.</p>
        </div>
        <div className="header-actions">
          {canManageAny && (
            <button type="button" className="btn btn-primary" onClick={() => setShowCreateForm((value) => !value)}>
              {showCreateForm ? 'Hide form' : 'Add device'}
            </button>
          )}
          <button type="button" className="btn btn-ghost" onClick={() => void refresh()}>
            Refresh
          </button>
        </div>
      </div>

      <div className="stat-grid">
        <StatCard label="Devices" value={totals.devices} />
        <StatCard label="OK" value={devicesOk} tone="ok" />
        <StatCard label="Alerting" value={devicesAlerting} tone={devicesAlerting > 0 ? 'danger' : 'default'} />
        <StatCard
          label="Offline"
          value={devicesOffline}
          tone={devicesOffline > 0 ? 'warning' : 'default'}
          hint="No reading for 30+ min"
        />
        <StatCard label="Companies" value={totals.companies} />
      </div>

      {error && <p className="error-banner">{error}</p>}

      {canManageAny && showCreateForm && (
        <form className="card stack" onSubmit={handleCreateDevice}>
          <h2>Add device</h2>
          <label>
            Company
            <select
              value={selectedCompanyId}
              onChange={(e) => setDeviceForm((current) => ({ ...current, companyId: e.target.value }))}
              required
            >
              {accessibleCompanies.map((company) => (
                <option key={company.id} value={company.id}>
                  {company.name}
                </option>
              ))}
            </select>
          </label>
          <label>
            Name
            <input
              value={deviceForm.name}
              onChange={(e) => setDeviceForm((current) => ({ ...current, name: e.target.value }))}
              required
            />
          </label>
          <label>
            Zone
            <input
              value={deviceForm.zoneName}
              onChange={(e) => setDeviceForm((current) => ({ ...current, zoneName: e.target.value }))}
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
                onChange={(e) => setDeviceForm((current) => ({ ...current, minTempC: e.target.value }))}
                required
              />
            </label>
            <label>
              Max °C
              <input
                type="number"
                step="0.1"
                value={deviceForm.maxTempC}
                onChange={(e) => setDeviceForm((current) => ({ ...current, maxTempC: e.target.value }))}
                required
              />
            </label>
          </div>
          <button type="submit" className="btn btn-primary" disabled={creating || !selectedCompanyId}>
            {creating ? 'Creating…' : 'Create device'}
          </button>
        </form>
      )}

      {createdDevice && (
        <div className="card stack highlight">
          <h3>Device key — save this once</h3>
          <code className="device-key">{createdDevice.deviceKey}</code>
          <div className="inline-actions">
            <button type="button" className="btn btn-secondary" onClick={() => void handleCopyKey(createdDevice.deviceKey)}>
              Copy key
            </button>
            <button type="button" className="btn btn-ghost" onClick={() => setCreatedDevice(null)}>
              Dismiss
            </button>
          </div>
        </div>
      )}

      <div className="card stack">
          <div className="panel-header">
            <h2>All devices</h2>
            <span className="muted small">{devices.length} total</span>
          </div>

          {loading && devices.length === 0 && <p className="muted">Loading…</p>}
          {!loading && devices.length === 0 && (
            <p className="muted">
              {summaries.length === 0
                ? 'No companies assigned yet.'
                : canManageAny
                  ? 'No devices yet. Create one using the form.'
                  : 'No devices registered yet.'}
            </p>
          )}

          <ul className="status-list">
            {devices.map(({ device, companyId, companyName, status }) => {
              const canManage = canManageCompany(companyId);
              return (
                <li key={device.id}>
                  <div className="status-row-wrap">
                    <Link
                      to={`/companies/${companyId}/devices/${device.id}`}
                      className={`status-row${deviceStatusRowClass(status.tone)}`}
                    >
                      <div>
                        <strong>{device.name}</strong>
                        <span className="muted small">
                          {companyName} · {device.zoneName} · {device.minTempC}°C – {device.maxTempC}°C
                        </span>
                      </div>
                      <div className="status-row-meta">
                        {status.latestTemp !== undefined && <span>{status.latestTemp}°C</span>}
                        <span className={`pill pill-${status.tone}`}>{status.label}</span>
                      </div>
                    </Link>
                    {canManage && (
                      <button
                        type="button"
                        className="btn btn-ghost btn-sm device-delete-btn"
                        disabled={deletingId === device.id}
                        onClick={() => void handleDeleteDevice(device, companyId)}
                      >
                        {deletingId === device.id ? 'Deleting…' : 'Delete'}
                      </button>
                    )}
                  </div>
                  <p className="muted small device-row-meta">Last reading {formatDateTime(device.lastReadingAtUtc)}</p>
                </li>
              );
            })}
          </ul>
      </div>
    </section>
  );
}
