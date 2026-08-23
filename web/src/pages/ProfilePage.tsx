import { useState, type FormEvent } from 'react';
import { api } from '../api/client';
import { useToast } from '../components/Toast';
import { useAuth } from '../auth/AuthContext';

export function ProfilePage() {
  const { token, userName, userId, roles, logout } = useAuth();
  const { pushToast } = useToast();
  const [oldPassword, setOldPassword] = useState('');
  const [newPassword, setNewPassword] = useState('');
  const [confirmPassword, setConfirmPassword] = useState('');
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);

  async function handleSubmit(event: FormEvent) {
    event.preventDefault();
    if (!token) return;

    if (newPassword !== confirmPassword) {
      setError('New passwords do not match');
      return;
    }

    setLoading(true);
    setError(null);

    const response = await api.changePassword(token, oldPassword, newPassword);
    setLoading(false);

    if (!response.success) {
      setError(response.message ?? 'Could not change password');
      return;
    }

    setOldPassword('');
    setNewPassword('');
    setConfirmPassword('');
    pushToast('Password changed successfully', 'success');
  }

  return (
    <section className="stack">
      <div className="page-header">
        <div>
          <h1>Profile</h1>
          <p className="muted">Account details and security settings.</p>
        </div>
      </div>

      <div className="grid two-col">
        <div className="card stack">
          <h2>Account</h2>
          <dl className="detail-list">
            <div>
              <dt>Username</dt>
              <dd>{userName ?? '—'}</dd>
            </div>
            <div>
              <dt>User ID</dt>
              <dd className="mono">{userId ?? '—'}</dd>
            </div>
            <div>
              <dt>Roles</dt>
              <dd>{roles.length > 0 ? roles.join(', ') : '—'}</dd>
            </div>
          </dl>
          <button type="button" className="btn btn-ghost" onClick={logout}>
            Sign out
          </button>
        </div>

        <form className="card stack" onSubmit={handleSubmit}>
          <h2>Change password</h2>
          <label>
            Current password
            <input type="password" value={oldPassword} onChange={(e) => setOldPassword(e.target.value)} required />
          </label>
          <label>
            New password
            <input type="password" value={newPassword} onChange={(e) => setNewPassword(e.target.value)} required />
          </label>
          <label>
            Confirm new password
            <input type="password" value={confirmPassword} onChange={(e) => setConfirmPassword(e.target.value)} required />
          </label>
          {error && <p className="error-banner">{error}</p>}
          <button type="submit" className="btn btn-primary" disabled={loading}>
            {loading ? 'Updating…' : 'Update password'}
          </button>
        </form>
      </div>
    </section>
  );
}
