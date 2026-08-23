import { useState, type FormEvent } from 'react';
import { Navigate, useNavigate } from 'react-router-dom';
import { api } from '../api/client';
import { AuthFooterLink, AuthShell } from '../components/AuthShell';
import { useToast } from '../components/Toast';
import { useAuth } from '../auth/AuthContext';

export function RegisterPage() {
  const { isAuthenticated } = useAuth();
  const { pushToast } = useToast();
  const navigate = useNavigate();
  const [form, setForm] = useState({
    userName: '',
    email: '',
    password: '',
    firstName: '',
    lastName: '',
    dateOfBirth: '',
  });
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);

  if (isAuthenticated) {
    return <Navigate to="/" replace />;
  }

  async function handleSubmit(event: FormEvent) {
    event.preventDefault();
    setLoading(true);
    setError(null);

    const response = await api.register({
      ...form,
      dateOfBirth: new Date(form.dateOfBirth).toISOString(),
    });
    setLoading(false);

    if (!response.success) {
      setError(response.message ?? 'Registration failed');
      return;
    }

    if (response.data?.emailConfirmationRequired) {
      pushToast('Account created. Check your email to confirm before signing in.', 'info');
    } else {
      pushToast('Account created. You can sign in now.', 'success');
    }

    navigate('/login');
  }

  return (
    <AuthShell
      wide
      title="Create account"
      subtitle="Register for SmartMonitoring"
      footer={
        <p className="muted small auth-footer-text">
          Already have an account? <AuthFooterLink to="/login">Sign in</AuthFooterLink>
        </p>
      }
    >
      <form className="stack auth-form" onSubmit={handleSubmit}>
        <div className="inline-fields">
          <label>
            First name
            <input value={form.firstName} onChange={(e) => setForm((f) => ({ ...f, firstName: e.target.value }))} required />
          </label>
          <label>
            Last name
            <input value={form.lastName} onChange={(e) => setForm((f) => ({ ...f, lastName: e.target.value }))} required />
          </label>
        </div>
        <label>
          Username
          <input value={form.userName} onChange={(e) => setForm((f) => ({ ...f, userName: e.target.value }))} required />
        </label>
        <label>
          Email
          <input type="email" value={form.email} onChange={(e) => setForm((f) => ({ ...f, email: e.target.value }))} required />
        </label>
        <label>
          Date of birth
          <input type="date" value={form.dateOfBirth} onChange={(e) => setForm((f) => ({ ...f, dateOfBirth: e.target.value }))} required />
        </label>
        <label>
          Password
          <input type="password" value={form.password} onChange={(e) => setForm((f) => ({ ...f, password: e.target.value }))} required />
        </label>
        {error && <p className="error-banner">{error}</p>}
        <button type="submit" className="btn btn-primary btn-block" disabled={loading}>
          {loading ? 'Creating account…' : 'Register'}
        </button>
      </form>
    </AuthShell>
  );
}
