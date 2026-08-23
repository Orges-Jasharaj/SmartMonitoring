import { useEffect, useState, type FormEvent } from 'react';
import { api } from '../api/client';
import { useToast } from '../components/Toast';
import { useAuth } from '../auth/AuthContext';

export function RolesAdminPage() {
  const { token, isAdmin } = useAuth();
  const { pushToast } = useToast();
  const [roles, setRoles] = useState<string[]>([]);
  const [newRole, setNewRole] = useState('');
  const [loading, setLoading] = useState(true);

  async function load() {
    if (!token) return;
    setLoading(true);
    const response = await api.getRoles(token);
    setRoles(response.data ?? []);
    setLoading(false);
  }

  useEffect(() => {
    void load();
  }, [token]);

  async function handleCreate(event: FormEvent) {
    event.preventDefault();
    if (!token || !newRole.trim()) return;

    const response = await api.createRole(token, newRole.trim());
    if (!response.success) {
      pushToast(response.message ?? 'Could not create role', 'error');
      return;
    }

    setNewRole('');
    pushToast('Role created', 'success');
    await load();
  }

  async function handleDelete(roleName: string) {
    if (!token) return;
    const response = await api.deleteRole(token, roleName);
    if (!response.success) {
      pushToast(response.message ?? 'Could not delete role', 'error');
      return;
    }

    pushToast(`Role "${roleName}" deleted`, 'success');
    await load();
  }

  if (!isAdmin) {
    return <p className="error-banner">Admin access required.</p>;
  }

  return (
    <section className="stack">
      <div className="page-header">
        <div>
          <h1>Roles</h1>
          <p className="muted">System roles used for authorization across services.</p>
        </div>
        <button type="button" className="btn btn-ghost" onClick={() => void load()}>Refresh</button>
      </div>

      <form className="card stack" onSubmit={handleCreate}>
        <h2>Create role</h2>
        <label>
          Role name
          <input value={newRole} onChange={(e) => setNewRole(e.target.value)} placeholder="e.g. Support" required />
        </label>
        <button type="submit" className="btn btn-secondary">Create role</button>
      </form>

      <div className="card stack">
        <h2>Existing roles</h2>
        {loading && <p className="muted">Loading…</p>}
        <ul className="role-list">
          {roles.map((role) => (
            <li key={role} className="role-row">
              <strong>{role}</strong>
              {role !== 'Admin' && (
                <button type="button" className="btn btn-ghost" onClick={() => void handleDelete(role)}>
                  Delete
                </button>
              )}
            </li>
          ))}
        </ul>
      </div>
    </section>
  );
}
