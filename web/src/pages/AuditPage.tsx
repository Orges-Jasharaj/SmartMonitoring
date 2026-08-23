import { useEffect, useState, type FormEvent } from 'react';
import { api } from '../api/client';
import type { AuditLog } from '../api/types';
import { useAuth } from '../auth/AuthContext';
import { formatDateTime } from '../utils/monitoring';

export function AuditPage() {
  const { token } = useAuth();
  const [logs, setLogs] = useState<AuditLog[]>([]);
  const [page, setPage] = useState(1);
  const [totalCount, setTotalCount] = useState(0);
  const [pageSize] = useState(25);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [filters, setFilters] = useState({
    serviceName: '',
    eventType: '',
    actorUserId: '',
    fromUtc: '',
    toUtc: '',
  });

  async function load(nextPage = page) {
    if (!token) return;
    setLoading(true);
    setError(null);

    const response = await api.getAuditLogs(token, {
      serviceName: filters.serviceName || undefined,
      eventType: filters.eventType || undefined,
      actorUserId: filters.actorUserId || undefined,
      fromUtc: filters.fromUtc ? new Date(filters.fromUtc).toISOString() : undefined,
      toUtc: filters.toUtc ? new Date(filters.toUtc).toISOString() : undefined,
      page: nextPage,
      pageSize,
    });

    setLoading(false);

    if (!response.success || !response.data) {
      setError(response.message ?? 'Failed to load audit logs');
      return;
    }

    setLogs(response.data.items);
    setTotalCount(response.data.totalCount);
    setPage(response.data.page);
  }

  useEffect(() => {
    void load(1);
  }, [token]);

  function handleFilterSubmit(event: FormEvent) {
    event.preventDefault();
    void load(1);
  }

  const totalPages = Math.max(1, Math.ceil(totalCount / pageSize));

  return (
    <section className="stack">
      <div className="page-header">
        <div>
          <h1>Audit log</h1>
          <p className="muted">Cross-service activity from Identity, Monitoring, and other services.</p>
        </div>
        <button type="button" className="btn btn-ghost" onClick={() => void load(page)}>Refresh</button>
      </div>

      <form className="card stack filter-form" onSubmit={handleFilterSubmit}>
        <h2>Filters</h2>
        <div className="filter-grid">
          <label>
            Service
            <input value={filters.serviceName} onChange={(e) => setFilters((f) => ({ ...f, serviceName: e.target.value }))} placeholder="IdentityService" />
          </label>
          <label>
            Event type
            <input value={filters.eventType} onChange={(e) => setFilters((f) => ({ ...f, eventType: e.target.value }))} placeholder="UserLogin" />
          </label>
          <label>
            Actor user ID
            <input value={filters.actorUserId} onChange={(e) => setFilters((f) => ({ ...f, actorUserId: e.target.value }))} />
          </label>
          <label>
            From
            <input type="datetime-local" value={filters.fromUtc} onChange={(e) => setFilters((f) => ({ ...f, fromUtc: e.target.value }))} />
          </label>
          <label>
            To
            <input type="datetime-local" value={filters.toUtc} onChange={(e) => setFilters((f) => ({ ...f, toUtc: e.target.value }))} />
          </label>
        </div>
        <button type="submit" className="btn btn-secondary">Apply filters</button>
      </form>

      {error && <p className="error-banner">{error}</p>}

      <div className="card stack">
        <div className="panel-header">
          <h2>Events</h2>
          <span className="muted small">{totalCount} total</span>
        </div>
        {loading && <p className="muted">Loading…</p>}
        <div className="table-wrap">
          <table>
            <thead>
              <tr>
                <th>Time</th>
                <th>Service</th>
                <th>Event</th>
                <th>Outcome</th>
                <th>Actor</th>
                <th>Detail</th>
              </tr>
            </thead>
            <tbody>
              {logs.map((log) => (
                <tr key={log.id}>
                  <td>{formatDateTime(log.occurredAtUtc)}</td>
                  <td>{log.serviceName}</td>
                  <td>{log.eventType}</td>
                  <td>
                    <span className={`pill ${log.outcome === 'Success' ? 'pill-ok' : 'pill-danger'}`}>{log.outcome}</span>
                  </td>
                  <td>{log.actorUserName ?? log.actorUserId ?? '—'}</td>
                  <td className="small">{log.detail ?? '—'}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
        <div className="pagination">
          <button type="button" className="btn btn-ghost" disabled={page <= 1} onClick={() => void load(page - 1)}>
            Previous
          </button>
          <span className="muted small">Page {page} of {totalPages}</span>
          <button type="button" className="btn btn-ghost" disabled={page >= totalPages} onClick={() => void load(page + 1)}>
            Next
          </button>
        </div>
      </div>
    </section>
  );
}
