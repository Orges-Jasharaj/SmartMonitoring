import { useEffect, useState, type FormEvent } from 'react';
import { api } from '../api/client';
import type { User } from '../api/types';
import { useToast } from '../components/Toast';
import { useAuth } from '../auth/AuthContext';

export function UsersAdminPage() {
  const { token, isAdmin } = useAuth();
  const { pushToast } = useToast();
  const [users, setUsers] = useState<User[]>([]);
  const [roles, setRoles] = useState<string[]>([]);
  const [loading, setLoading] = useState(true);
  const [assignUserId, setAssignUserId] = useState('');
  const [assignRole, setAssignRole] = useState('');

  async function load() {
    if (!token) return;
    setLoading(true);
    const [usersRes, rolesRes] = await Promise.all([api.getUsers(token), api.getRoles(token)]);
    setUsers(usersRes.data ?? []);
    setRoles(rolesRes.data ?? []);
    setLoading(false);
  }

  useEffect(() => {
    void load();
  }, [token]);

  async function toggleActive(user: User) {
    if (!token) return;
    const response = user.isActive
      ? await api.deactivateUser(token, user.id)
      : await api.activateUser(token, user.id);

    if (!response.success) {
      pushToast(response.message ?? 'Action failed', 'error');
      return;
    }

    pushToast(`User ${user.userName} ${user.isActive ? 'deactivated' : 'activated'}`, 'success');
    await load();
  }

  async function handleAssignRole(event: FormEvent) {
    event.preventDefault();
    if (!token || !assignUserId || !assignRole) return;

    const response = await api.assignRole(token, assignUserId, assignRole);
    if (!response.success) {
      pushToast(response.message ?? 'Could not assign role', 'error');
      return;
    }

    pushToast('Role assigned', 'success');
    setAssignUserId('');
    setAssignRole('');
    await load();
  }

  if (!isAdmin) {
    return <p className="error-banner">Admin access required.</p>;
  }

  return (
    <section className="stack">
      <div className="page-header">
        <div>
          <h1>Users</h1>
          <p className="muted">Manage identity users, activation, and system roles.</p>
        </div>
        <button type="button" className="btn btn-ghost" onClick={() => void load()}>Refresh</button>
      </div>

      <form className="card stack" onSubmit={handleAssignRole}>
        <h2>Assign system role</h2>
        <div className="inline-fields">
          <label>
            User
            <select value={assignUserId} onChange={(e) => setAssignUserId(e.target.value)} required>
              <option value="">Select user…</option>
              {users.map((user) => (
                <option key={user.id} value={user.id}>{user.userName}</option>
              ))}
            </select>
          </label>
          <label>
            Role
            <select value={assignRole} onChange={(e) => setAssignRole(e.target.value)} required>
              <option value="">Select role…</option>
              {roles.map((role) => (
                <option key={role} value={role}>{role}</option>
              ))}
            </select>
          </label>
        </div>
        <button type="submit" className="btn btn-secondary">Assign role</button>
      </form>

      <div className="card stack">
        <h2>All users</h2>
        {loading && <p className="muted">Loading…</p>}
        <div className="table-wrap">
          <table>
            <thead>
              <tr>
                <th>Username</th>
                <th>Email</th>
                <th>Roles</th>
                <th>Status</th>
                <th />
              </tr>
            </thead>
            <tbody>
              {users.map((user) => (
                <tr key={user.id}>
                  <td>{user.userName}</td>
                  <td>{user.email}</td>
                  <td>{user.roles.join(', ') || '—'}</td>
                  <td>
                    <span className={`pill ${user.isActive ? 'pill-ok' : 'pill-muted'}`}>
                      {user.isActive ? 'Active' : 'Inactive'}
                    </span>
                  </td>
                  <td>
                    <button type="button" className="btn btn-ghost" onClick={() => void toggleActive(user)}>
                      {user.isActive ? 'Deactivate' : 'Activate'}
                    </button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </div>
    </section>
  );
}
