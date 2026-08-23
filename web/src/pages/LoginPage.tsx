import { useState, type FormEvent } from 'react';
import { Navigate, useNavigate } from 'react-router-dom';
import { api } from '../api/client';
import { AuthFooterLink, AuthShell } from '../components/AuthShell';
import { useToast } from '../components/Toast';
import { useAuth } from '../auth/AuthContext';

export function LoginPage() {
  const { login, isAuthenticated } = useAuth();
  const { pushToast } = useToast();
  const navigate = useNavigate();
  const [userNameOrEmail, setUserNameOrEmail] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);

  if (isAuthenticated) {
    return <Navigate to="/" replace />;
  }

  async function handleSubmit(event: FormEvent) {
    event.preventDefault();
    setLoading(true);
    setError(null);

    const response = await api.login(userNameOrEmail, password);
    setLoading(false);

    if (!response.success || !response.data?.token) {
      setError(response.message ?? response.errors?.[0]?.errorMessage ?? 'Login failed');
      return;
    }

    login(response.data.token, response.data.expiresAt);
    pushToast('Welcome back!', 'success');
    navigate('/');
  }

  return (
    <AuthShell
      title="Sign in"
      subtitle="Access your monitoring dashboard"
      footer={
        <p className="muted small auth-footer-text">
          No account? <AuthFooterLink to="/register">Create one</AuthFooterLink>
          {' · '}
          <AuthFooterLink to="/forgot-password">Forgot password?</AuthFooterLink>
        </p>
      }
    >
      <form className="stack auth-form" onSubmit={handleSubmit}>
        <label>
          Username or email
          <input
            value={userNameOrEmail}
            onChange={(e) => setUserNameOrEmail(e.target.value)}
            autoComplete="username"
            placeholder="you@company.com"
            required
          />
        </label>

        <label>
          Password
          <input
            type="password"
            value={password}
            onChange={(e) => setPassword(e.target.value)}
            autoComplete="current-password"
            placeholder="••••••••"
            required
          />
        </label>

        {error && <p className="error-banner">{error}</p>}

        <button type="submit" className="btn btn-primary btn-block" disabled={loading}>
          {loading ? 'Signing in…' : 'Sign in'}
        </button>
      </form>
    </AuthShell>
  );
}
