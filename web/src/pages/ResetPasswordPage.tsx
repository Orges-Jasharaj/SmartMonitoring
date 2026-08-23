import { useEffect, useState, type FormEvent } from 'react';
import { Navigate, useNavigate, useSearchParams } from 'react-router-dom';
import { api } from '../api/client';
import { AuthFooterLink, AuthShell } from '../components/AuthShell';
import { useToast } from '../components/Toast';
import { useAuth } from '../auth/AuthContext';

export function ResetPasswordPage() {
  const { isAuthenticated } = useAuth();
  const { pushToast } = useToast();
  const navigate = useNavigate();
  const [searchParams] = useSearchParams();
  const [userId, setUserId] = useState('');
  const [token, setToken] = useState('');
  const [newPassword, setNewPassword] = useState('');
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);

  useEffect(() => {
    setUserId(searchParams.get('userId') ?? '');
    setToken(searchParams.get('token') ?? '');
  }, [searchParams]);

  if (isAuthenticated) {
    return <Navigate to="/" replace />;
  }

  async function handleSubmit(event: FormEvent) {
    event.preventDefault();
    setLoading(true);
    setError(null);

    const response = await api.resetPassword(userId.trim(), token.trim(), newPassword);
    setLoading(false);

    if (!response.success) {
      setError(response.message ?? 'Reset failed');
      return;
    }

    pushToast('Password updated. You can sign in now.', 'success');
    navigate('/login');
  }

  return (
    <AuthShell
      title="Reset password"
      subtitle="Enter the token from your email"
      footer={
        <p className="muted small auth-footer-text">
          Need a new link? <AuthFooterLink to="/forgot-password">Request one</AuthFooterLink>
        </p>
      }
    >
      <form className="stack auth-form" onSubmit={handleSubmit}>
        <label>
          User ID
          <input value={userId} onChange={(e) => setUserId(e.target.value)} required />
        </label>
        <label>
          Reset token
          <input value={token} onChange={(e) => setToken(e.target.value)} required />
        </label>
        <label>
          New password
          <input type="password" value={newPassword} onChange={(e) => setNewPassword(e.target.value)} required />
        </label>
        <p className="muted small">
          Open the reset link from Mailpit, or paste <strong>userId</strong> and <strong>token</strong> from the URL here.
        </p>
        {error && <p className="error-banner">{error}</p>}
        <button type="submit" className="btn btn-primary btn-block" disabled={loading}>
          {loading ? 'Updating…' : 'Update password'}
        </button>
      </form>
    </AuthShell>
  );
}
