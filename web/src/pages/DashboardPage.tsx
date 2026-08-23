import { useEffect, useState, type FormEvent } from 'react';
import { Link } from 'react-router-dom';
import { api } from '../api/client';
import type { Company, CompanySummary } from '../api/types';
import { StatCard } from '../components/StatCard';
import { useToast } from '../components/Toast';
import { useAuth } from '../auth/AuthContext';
import { getDeviceStatus } from '../utils/monitoring';

export function DashboardPage() {
  const { token } = useAuth();
  const { pushToast } = useToast();
  const [summaries, setSummaries] = useState<CompanySummary[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [newCompanyName, setNewCompanyName] = useState('');
  const [creating, setCreating] = useState(false);

  async function loadDashboard() {
    if (!token) return;
    setLoading(true);
    setError(null);

    const companiesRes = await api.getCompanies(token);
    if (!companiesRes.success || !companiesRes.data) {
      setLoading(false);
      setError(companiesRes.message ?? 'Failed to load companies');
      return;
    }

    const enriched = await Promise.all(
      companiesRes.data.map(async (company) => buildSummary(token, company)),
    );

    setSummaries(enriched);
    setLoading(false);
  }

  useEffect(() => {
    void loadDashboard();
  }, [token]);

  async function handleCreate(event: FormEvent) {
    event.preventDefault();
    if (!token || !newCompanyName.trim()) return;

    setCreating(true);
    const response = await api.createCompany(token, newCompanyName.trim());
    setCreating(false);

    if (!response.success) {
      pushToast(response.message ?? 'Could not create company (admin role required)', 'error');
      return;
    }

    setNewCompanyName('');
    pushToast(`Company "${response.data?.name}" created`, 'success');
    await loadDashboard();
  }

  const totals = summaries.reduce(
    (acc, item) => ({
      companies: acc.companies + 1,
      devices: acc.devices + item.deviceCount,
      alerts: acc.alerts + item.activeAlerts,
    }),
    { companies: 0, devices: 0, alerts: 0 },
  );

  return (
    <section className="stack">
      <div className="page-header">
        <div>
          <h1>Dashboard</h1>
          <p>Overview of your monitored companies, devices, and active alerts.</p>
        </div>
        <button type="button" className="btn btn-ghost" onClick={() => void loadDashboard()}>
          Refresh
        </button>
      </div>

      <div className="stat-grid">
        <StatCard label="Companies" value={totals.companies} />
        <StatCard label="Devices" value={totals.devices} />
        <StatCard label="Active alerts" value={totals.alerts} tone={totals.alerts > 0 ? 'danger' : 'ok'} />
      </div>

      <div className="grid two-col">
        <div className="card stack">
          <h2>Companies</h2>
          {loading && <p className="muted">Loading…</p>}
          {error && <p className="error-banner">{error}</p>}
          {!loading && summaries.length === 0 && (
            <p className="muted">No companies yet. Create one if you are a system admin, or ask to be assigned.</p>
          )}
          <div className="company-grid">
            {summaries.map(({ company, deviceCount, activeAlerts, devicesOk, devicesAlerting }) => (
              <Link key={company.id} to={`/companies/${company.id}`} className="company-card">
                <div className="company-card-top">
                  <strong>{company.name}</strong>
                  {activeAlerts > 0 && <span className="pill pill-danger">{activeAlerts} alert{activeAlerts > 1 ? 's' : ''}</span>}
                </div>
                <div className="company-card-stats">
                  <span>{deviceCount} devices</span>
                  <span className="ok-text">{devicesOk} OK</span>
                  {devicesAlerting > 0 && <span className="danger-text">{devicesAlerting} alerting</span>}
                </div>
                <span className="muted small">Created {new Date(company.createdAtUtc).toLocaleDateString()}</span>
              </Link>
            ))}
          </div>
        </div>

        <form className="card stack" onSubmit={handleCreate}>
          <h2>Create company</h2>
          <p className="muted">System administrators can register a new tenant.</p>
          <label>
            Company name
            <input
              value={newCompanyName}
              onChange={(e) => setNewCompanyName(e.target.value)}
              placeholder="e.g. City Pharmacy"
            />
          </label>
          <button type="submit" className="btn btn-secondary" disabled={creating || !newCompanyName.trim()}>
            {creating ? 'Creating…' : 'Create company'}
          </button>
        </form>
      </div>
    </section>
  );
}

async function buildSummary(token: string, company: Company): Promise<CompanySummary> {
  const [devicesRes, alertsRes, readingsRes] = await Promise.all([
    api.getDevices(token, company.id),
    api.getAlerts(token, company.id, true),
    api.getReadings(token, company.id, undefined, 50),
  ]);

  const devices = devicesRes.data ?? [];
  const readings = readingsRes.data ?? [];
  const devicesOk = devices.filter((d) => getDeviceStatus(d, readings).tone === 'ok').length;
  const devicesAlerting = devices.filter((d) => getDeviceStatus(d, readings).tone === 'danger').length;

  return {
    company,
    deviceCount: devices.length,
    activeAlerts: alertsRes.data?.length ?? 0,
    devicesOk,
    devicesAlerting,
  };
}
