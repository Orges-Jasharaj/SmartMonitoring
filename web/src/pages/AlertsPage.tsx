import { useCallback, useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { api } from '../api/client';
import type { Alert } from '../api/types';
import { StatCard } from '../components/StatCard';
import { AlertAcknowledgeButton } from '../components/AlertAcknowledgeButton';
import { Pagination } from '../components/Pagination';
import { useToast } from '../components/Toast';
import { useAuth } from '../auth/AuthContext';
import { useGlobalMonitoring, type GlobalAlert } from '../hooks/useGlobalMonitoring';
import { formatDateTime, paginateClientList } from '../utils/monitoring';

const PAGE_SIZE = 25;

export function AlertsPage() {
  const { token } = useAuth();
  const { pushToast } = useToast();
  const { alerts: activeAlerts, loading: monitoringLoading, error, refresh: refreshMonitoring, totals } =
    useGlobalMonitoring(token);
  const [showHistory, setShowHistory] = useState(false);
  const [page, setPage] = useState(1);
  const [historyAlerts, setHistoryAlerts] = useState<GlobalAlert[]>([]);
  const [historyLoading, setHistoryLoading] = useState(false);

  const loadHistory = useCallback(async () => {
    if (!token) return;
    setHistoryLoading(true);

    const companiesRes = await api.getCompanies(token);
    if (!companiesRes.success || !companiesRes.data) {
      setHistoryAlerts([]);
      setHistoryLoading(false);
      return;
    }

    const nested = await Promise.all(
      companiesRes.data.map(async (company) => {
        const [alertsRes, devicesRes] = await Promise.all([
          api.getAlerts(token, company.id, { activeOnly: false, page: 1, pageSize: 200 }),
          api.getDevices(token, company.id, { page: 1, pageSize: 200 }),
        ]);
        const deviceNames = new Map((devicesRes.data?.items ?? []).map((device) => [device.id, device.name]));
        return (alertsRes.data?.items ?? []).map((alert: Alert) => ({
          alert,
          companyId: company.id,
          companyName: company.name,
          deviceName: deviceNames.get(alert.deviceId) ?? alert.deviceId.slice(0, 8),
        }));
      }),
    );

    setHistoryAlerts(
      nested
        .flat()
        .sort(
          (a, b) =>
            new Date(b.alert.triggeredAtUtc).getTime() - new Date(a.alert.triggeredAtUtc).getTime(),
        ),
    );
    setHistoryLoading(false);
  }, [token]);

  useEffect(() => {
    if (!showHistory) return;
    void loadHistory();
  }, [showHistory, loadHistory]);

  useEffect(() => {
    setPage(1);
  }, [showHistory]);

  const sourceAlerts = showHistory ? historyAlerts : activeAlerts;
  const pagedAlerts = paginateClientList(sourceAlerts, page, PAGE_SIZE);
  const loading = showHistory ? historyLoading : monitoringLoading;

  async function handleAcknowledged() {
    pushToast('Alert acknowledged', 'success');
    void refreshMonitoring();
    if (showHistory) {
      void loadHistory();
    }
  }

  function handleAcknowledgeError(message: string) {
    pushToast(message, 'error');
  }

  return (
    <section className="stack" data-testid="page-alerts">
      <div className="page-header">
        <div>
          <h1>Alerts</h1>
          <p>
            {showHistory
              ? 'Resolved and past alerts across all your companies.'
              : 'Active temperature alerts across all your companies.'}
          </p>
        </div>
        <button
          type="button"
          className="btn btn-ghost"
          onClick={() => {
            void refreshMonitoring();
            if (showHistory) void loadHistory();
          }}
        >
          Refresh
        </button>
      </div>

      <div className="stat-grid">
        <StatCard label="Active alerts" value={totals.alerts} tone={totals.alerts > 0 ? 'danger' : 'ok'} />
        <StatCard label="Companies" value={totals.companies} />
        <StatCard label="Devices" value={totals.devices} />
      </div>

      {error && <p className="error-banner">{error}</p>}

      <div className="card stack">
        <div className="panel-header">
          <h2>{showHistory ? 'Alert history' : 'Active alerts'}</h2>
          <div className="panel-header-actions">
            <span className="muted small">{pagedAlerts.totalCount} total</span>
            <button type="button" className="btn btn-ghost" onClick={() => setShowHistory((value) => !value)}>
              {showHistory ? 'Show active only' : 'Show history'}
            </button>
          </div>
        </div>

        {loading && pagedAlerts.items.length === 0 && <p className="muted">Loading…</p>}
        {!loading && pagedAlerts.items.length === 0 && (
          <p className="muted">
            {showHistory ? 'No alert history yet.' : 'No active alerts. All monitored devices are within range.'}
          </p>
        )}

        <ul className="alert-list">
          {pagedAlerts.items.map(({ alert, companyId, companyName, deviceName }) => (
            <li key={alert.id} className={`alert-item${alert.isActive ? ' alert-item-active' : ' resolved'}`}>
              <div className="alert-item-row">
                <Link to={`/companies/${companyId}?tab=alerts`} className="dashboard-link-block alert-item-body">
                  <div className="notification-item-top">
                    <div>
                      <strong>{deviceName}</strong>
                      <span className="muted small"> · {companyName}</span>
                    </div>
                    <span className={`pill ${alert.isActive ? 'pill-danger' : 'pill-muted'}`}>{alert.alertType}</span>
                  </div>
                  <p className="small">{alert.message}</p>
                  <p className="muted small">Triggered {formatDateTime(alert.triggeredAtUtc)}</p>
                  {!alert.isActive && alert.resolvedAtUtc && (
                    <p className="muted small">Resolved {formatDateTime(alert.resolvedAtUtc)}</p>
                  )}
                </Link>
                <AlertAcknowledgeButton
                  token={token}
                  companyId={companyId}
                  alert={alert}
                  onAcknowledged={handleAcknowledged}
                  onError={handleAcknowledgeError}
                />
              </div>
            </li>
          ))}
        </ul>
        <Pagination
          page={page}
          pageSize={PAGE_SIZE}
          totalCount={pagedAlerts.totalCount}
          loading={loading}
          onPageChange={setPage}
        />
      </div>
    </section>
  );
}
