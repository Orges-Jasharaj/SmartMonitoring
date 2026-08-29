import { useState } from 'react';
import { Link } from 'react-router-dom';
import { StatCard } from '../components/StatCard';
import { useAuth } from '../auth/AuthContext';
import { useGlobalMonitoring } from '../hooks/useGlobalMonitoring';
import { formatDateTime } from '../utils/monitoring';

export function AlertsPage() {
  const { token } = useAuth();
  const { alerts, alertHistory, loading, error, refresh, totals } = useGlobalMonitoring(token);
  const [showHistory, setShowHistory] = useState(false);

  const visibleAlerts = showHistory ? alertHistory : alerts;

  return (
    <section className="stack">
      <div className="page-header">
        <div>
          <h1>Alerts</h1>
          <p>
            {showHistory
              ? 'Resolved and past alerts across all your companies.'
              : 'Active temperature alerts across all your companies.'}
          </p>
        </div>
        <button type="button" className="btn btn-ghost" onClick={() => void refresh()}>
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
            <span className="muted small">{visibleAlerts.length} total</span>
            <button type="button" className="btn btn-ghost" onClick={() => setShowHistory((value) => !value)}>
              {showHistory ? 'Show active only' : 'Show history'}
            </button>
          </div>
        </div>

        {loading && visibleAlerts.length === 0 && <p className="muted">Loading…</p>}
        {!loading && visibleAlerts.length === 0 && (
          <p className="muted">
            {showHistory ? 'No alert history yet.' : 'No active alerts. All monitored devices are within range.'}
          </p>
        )}

        <ul className="alert-list">
          {visibleAlerts.map(({ alert, companyId, companyName, deviceName }) => (
            <li key={alert.id} className={`alert-item${alert.isActive ? ' alert-item-active' : ' resolved'}`}>
              <Link to={`/companies/${companyId}?tab=alerts`} className="dashboard-link-block">
                <div className="notification-item-top">
                  <div>
                    <strong>{deviceName}</strong>
                    <span className="muted small"> · {companyName}</span>
                  </div>
                  <span className={`pill ${alert.isActive ? 'pill-danger' : 'pill-muted'}`}>{alert.alertType}</span>
                </div>
                <p className="small">{alert.message}</p>
                {alert.temperatureC != null && (
                  <p className={`small${alert.isActive ? ' danger-text' : ''}`}>{alert.temperatureC}°C</p>
                )}
                <p className="muted small">Triggered {formatDateTime(alert.triggeredAtUtc)}</p>
                {!alert.isActive && alert.resolvedAtUtc && (
                  <p className="muted small">Resolved {formatDateTime(alert.resolvedAtUtc)}</p>
                )}
              </Link>
            </li>
          ))}
        </ul>
      </div>
    </section>
  );
}
