import { useEffect, useState, type FormEvent } from 'react';
import { Link } from 'react-router-dom';
import { api } from '../api/client';
import type { Company } from '../api/types';
import { useAuth } from '../auth/AuthContext';

export function DashboardPage() {
  const { token } = useAuth();
  const [companies, setCompanies] = useState<Company[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [newCompanyName, setNewCompanyName] = useState('');
  const [creating, setCreating] = useState(false);

  async function loadCompanies() {
    if (!token) return;
    setLoading(true);
    setError(null);
    const response = await api.getCompanies(token);
    setLoading(false);

    if (!response.success || !response.data) {
      setError(response.message ?? 'Failed to load companies');
      return;
    }

    setCompanies(response.data);
  }

  useEffect(() => {
    void loadCompanies();
  }, [token]);

  async function handleCreate(event: FormEvent) {
    event.preventDefault();
    if (!token || !newCompanyName.trim()) return;

    setCreating(true);
    const response = await api.createCompany(token, newCompanyName.trim());
    setCreating(false);

    if (!response.success) {
      setError(response.message ?? 'Could not create company (admin role required)');
      return;
    }

    setNewCompanyName('');
    await loadCompanies();
  }

  return (
    <section className="stack">
      <div className="page-header">
        <div>
          <h1>Companies</h1>
          <p>Select a company to view devices, readings, and alerts.</p>
        </div>
      </div>

      <div className="grid two-col">
        <div className="card stack">
          <h2>Your companies</h2>
          {loading && <p className="muted">Loading…</p>}
          {error && <p className="error-banner">{error}</p>}
          {!loading && companies.length === 0 && (
            <p className="muted">No companies yet. Create one if you are a system admin, or ask to be assigned.</p>
          )}
          <ul className="company-list">
            {companies.map((company) => (
              <li key={company.id}>
                <Link to={`/companies/${company.id}`} className="company-link">
                  <strong>{company.name}</strong>
                  <span>{new Date(company.createdAtUtc).toLocaleDateString()}</span>
                </Link>
              </li>
            ))}
          </ul>
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
