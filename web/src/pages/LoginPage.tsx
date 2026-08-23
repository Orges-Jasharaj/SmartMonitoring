import { useState, type FormEvent } from 'react';
import { Navigate, useNavigate } from 'react-router-dom';
import { api } from '../api/client';
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
    <div className="login-page">
      <form className="login-card card" onSubmit={handleSubmit}>
        <div className="brand compact">
          <span className="brand-mark" aria-hidden="true" />
          <div>
            <strong>SmartMonitoring</strong>
            <span>Sign in to your dashboard</span>
          </div>
        </div>

        <label>
          Username or email
          <input
            value={userNameOrEmail}
            onChange={(e) => setUserNameOrEmail(e.target.value)}
            autoComplete="username"
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
            required
          />
        </label>

        {error && <p className="error-banner">{error}</p>}

        <button type="submit" className="btn btn-primary" disabled={loading}>
          {loading ? 'Signing in…' : 'Sign in'}
        </button>
      </form>
    </div>
  );
}
