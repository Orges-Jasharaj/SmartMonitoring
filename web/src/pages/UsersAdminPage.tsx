import { useEffect, useState, type FormEvent } from 'react';
import { api } from '../api/client';
import type { User } from '../api/types';
import { Pagination } from '../components/Pagination';
import { useToast } from '../components/Toast';
import { useAuth } from '../auth/AuthContext';

const PAGE_SIZE = 25;

export function UsersAdminPage() {
  const { token, isAdmin } = useAuth();
  const { pushToast } = useToast();
  const [users, setUsers] = useState<User[]>([]);
  const [assignUsers, setAssignUsers] = useState<User[]>([]);
  const [roles, setRoles] = useState<string[]>([]);
  const [loading, setLoading] = useState(true);
  const [page, setPage] = useState(1);
  const [totalCount, setTotalCount] = useState(0);
  const [search, setSearch] = useState('');
  const [assignUserId, setAssignUserId] = useState('');
  const [assignRole, setAssignRole] = useState('');

  async function load(nextPage = page, nextSearch = search) {
    if (!token) return;
    setLoading(true);
    const [usersRes, rolesRes, assignUsersRes] = await Promise.all([
      api.getUsers(token, { page: nextPage, pageSize: PAGE_SIZE, search: nextSearch || undefined }),
      api.getRoles(token),
      api.getUsers(token, { page: 1, pageSize: 200 }),
    ]);
    setUsers(usersRes.data?.items ?? []);
    setTotalCount(usersRes.data?.totalCount ?? 0);
    setPage(usersRes.data?.page ?? nextPage);
    setAssignUsers(assignUsersRes.data?.items ?? []);
    setRoles(rolesRes.data ?? []);
    setLoading(false);
  }

  useEffect(() => {
    void load(1, search);
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
    await load(page, search);
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
    await load(page, search);
  }

  function handleSearchSubmit(event: FormEvent) {
    event.preventDefault();
    void load(1, search);
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
        <button type="button" className="btn btn-ghost" onClick={() => void load(page, search)}>Refresh</button>
      </div>

      <form className="card stack" onSubmit={handleAssignRole}>
        <h2>Assign system role</h2>
        <div className="inline-fields">
          <label>
            User
            <select value={assignUserId} onChange={(e) => setAssignUserId(e.target.value)} required>
              <option value="">Select user…</option>
              {assignUsers.map((user) => (
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
        <div className="panel-header">
          <h2>All users</h2>
          <form className="inline-fields" onSubmit={handleSearchSubmit}>
            <input
              value={search}
              onChange={(e) => setSearch(e.target.value)}
              placeholder="Search username or email…"
            />
            <button type="submit" className="btn btn-secondary">Search</button>
          </form>
        </div>
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
        <Pagination
          page={page}
          pageSize={PAGE_SIZE}
          totalCount={totalCount}
          loading={loading}
          onPageChange={(nextPage) => void load(nextPage, search)}
        />
      </div>
    </section>
  );
}
