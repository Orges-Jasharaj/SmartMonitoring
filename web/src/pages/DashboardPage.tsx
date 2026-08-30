import { useEffect, useState, type FormEvent } from 'react';
import { Link } from 'react-router-dom';
import { api } from '../api/client';
import type { User } from '../api/types';
import { StatCard } from '../components/StatCard';
import { useToast } from '../components/Toast';
import { useAuth } from '../auth/AuthContext';
import { useGlobalMonitoring } from '../hooks/useGlobalMonitoring';

export function DashboardPage() {
  const { token, isAdmin } = useAuth();
  const { pushToast } = useToast();
  const { summaries, loading, error, refresh, totals } = useGlobalMonitoring(token);
  const [newCompanyName, setNewCompanyName] = useState('');
  const [initialAdminUserId, setInitialAdminUserId] = useState('');
  const [users, setUsers] = useState<User[]>([]);
  const [creating, setCreating] = useState(false);
  const [deletingCompanyId, setDeletingCompanyId] = useState<string | null>(null);

  useEffect(() => {
    if (!token || !isAdmin) {
      setUsers([]);
      return;
    }

    void api.getUsers(token).then((response) => {
      setUsers(response.data ?? []);
    });
  }, [token, isAdmin]);

  async function handleCreate(event: FormEvent) {
    event.preventDefault();
    if (!token || !newCompanyName.trim()) return;

    setCreating(true);
    const response = await api.createCompany(
      token,
      newCompanyName.trim(),
      initialAdminUserId || undefined,
    );
    setCreating(false);

    if (!response.success) {
      pushToast(response.message ?? 'Could not create company (admin role required)', 'error');
      return;
    }

    setNewCompanyName('');
    setInitialAdminUserId('');
    pushToast(`Company "${response.data?.name}" created`, 'success');
    await refresh();
  }

  async function handleDeleteCompany(companyId: string, companyName: string) {
    if (!token) return;
    if (
      !window.confirm(
        `Delete company "${companyName}"? This permanently removes its devices, readings, alerts, and team assignments.`,
      )
    ) {
      return;
    }

    setDeletingCompanyId(companyId);
    const response = await api.deleteCompany(token, companyId);
    setDeletingCompanyId(null);

    if (!response.success) {
      pushToast(response.message ?? 'Failed to delete company', 'error');
      return;
    }

    pushToast(`Company "${companyName}" deleted`, 'success');
    await refresh();
  }

  return (
    <section className="stack">
      <div className="page-header">
        <div>
          <h1>Dashboard</h1>
          <p>Overview of your monitored companies.</p>
        </div>
        <button type="button" className="btn btn-ghost" onClick={() => void refresh()}>
          Refresh
        </button>
      </div>

      <div className="stat-grid">
        <StatCard label="Companies" value={totals.companies} />
        <StatCard label="Devices" value={totals.devices} />
        <StatCard label="Active alerts" value={totals.alerts} tone={totals.alerts > 0 ? 'danger' : 'ok'} />
      </div>

      {error && <p className="error-banner">{error}</p>}

      <div className={`grid ${isAdmin ? 'two-col' : 'single-col'}`}>
        <div className="card stack">
          <div className="panel-header">
            <h2>Companies</h2>
            {!loading && summaries.length > 0 && (
              <span className="muted small">{summaries.length} tenant{summaries.length === 1 ? '' : 's'}</span>
            )}
          </div>
          {loading && summaries.length === 0 && <p className="muted">Loading…</p>}
          {!loading && summaries.length === 0 && (
            <p className="muted">No companies yet. Create one if you are a system admin, or ask to be assigned.</p>
          )}
          <div className="company-grid">
            {summaries.map(({ company, deviceCount, activeAlerts, devicesOk, devicesAlerting, devicesOffline }) => (
              <div
                key={company.id}
                className={`company-card${activeAlerts > 0 ? ' company-card-alert' : ''}`}
              >
                <Link to={`/companies/${company.id}`} className="company-card-body link-card">
                  <div className="company-card-top">
                    <strong>{company.name}</strong>
                    {activeAlerts > 0 && (
                      <span className="pill pill-danger">
                        {activeAlerts} alert{activeAlerts > 1 ? 's' : ''}
                      </span>
                    )}
                  </div>
                  <div className="company-card-stats">
                    <span>{deviceCount} devices</span>
                  <span className="ok-text">{devicesOk} OK</span>
                  {devicesOffline > 0 && <span className="warning-text">{devicesOffline} offline</span>}
                  {devicesAlerting > 0 && <span className="danger-text">{devicesAlerting} alerting</span>}
                  </div>
                  <span className="muted small">Created {new Date(company.createdAtUtc).toLocaleDateString()}</span>
                </Link>
                {isAdmin && (
                  <button
                    type="button"
                    className="btn btn-ghost btn-sm company-delete-btn"
                    disabled={deletingCompanyId === company.id}
                    onClick={() => void handleDeleteCompany(company.id, company.name)}
                  >
                    {deletingCompanyId === company.id ? 'Deleting…' : 'Delete'}
                  </button>
                )}
              </div>
            ))}
          </div>
        </div>

        {isAdmin && (
          <form className="card stack highlight" onSubmit={handleCreate}>
            <h2>Create company</h2>
            <p className="muted">Register a new tenant organization.</p>
            <label>
              Company name
              <input
                value={newCompanyName}
                onChange={(e) => setNewCompanyName(e.target.value)}
                placeholder="e.g. City Pharmacy"
              />
            </label>
            <label>
              Company admin
              <select value={initialAdminUserId} onChange={(e) => setInitialAdminUserId(e.target.value)}>
                <option value="">Assign yourself as company admin</option>
                {users.map((user) => (
                  <option key={user.id} value={user.id}>
                    {user.userName} ({user.email})
                  </option>
                ))}
              </select>
            </label>
            <p className="muted small">
              The selected user is automatically assigned as CompanyAdmin and can manage devices and team members.
            </p>
            <button type="submit" className="btn btn-primary" disabled={creating || !newCompanyName.trim()}>
              {creating ? 'Creating…' : 'Create company'}
            </button>
          </form>
        )}
      </div>
    </section>
  );
}
