import { useState, type FormEvent } from 'react';
import { Link, Navigate } from 'react-router-dom';
import { api } from '../api/client';
import { AuthFooterLink, AuthShell } from '../components/AuthShell';
import { useToast } from '../components/Toast';
import { useAuth } from '../auth/AuthContext';

export function ForgotPasswordPage() {
  const { isAuthenticated } = useAuth();
  const { pushToast } = useToast();
  const [emailOrUserName, setEmailOrUserName] = useState('');
  const [error, setError] = useState<string | null>(null);
  const [sent, setSent] = useState(false);
  const [loading, setLoading] = useState(false);

  if (isAuthenticated) {
    return <Navigate to="/" replace />;
  }

  async function handleSubmit(event: FormEvent) {
    event.preventDefault();
    setLoading(true);
    setError(null);

    const response = await api.forgotPassword(emailOrUserName.trim());
    setLoading(false);

    if (!response.success) {
      setError(response.message ?? 'Request failed');
      return;
    }

    setSent(true);
    pushToast('If the account exists, a reset email was sent. Check Mailpit in dev.', 'info');
  }

  return (
    <AuthShell
      title="Forgot password"
      subtitle="We will email you a reset link"
      footer={
        <p className="muted small auth-footer-text">
          Remembered it? <AuthFooterLink to="/login">Back to sign in</AuthFooterLink>
        </p>
      }
    >
      {sent ? (
        <div className="stack">
          <p className="success-banner">If an account exists for that email or username, a reset link has been sent.</p>
          <Link to="/login" className="btn btn-secondary btn-block">Back to sign in</Link>
        </div>
      ) : (
        <form className="stack auth-form" onSubmit={handleSubmit}>
          <label>
            Email or username
            <input value={emailOrUserName} onChange={(e) => setEmailOrUserName(e.target.value)} required />
          </label>
          {error && <p className="error-banner">{error}</p>}
          <button type="submit" className="btn btn-primary btn-block" disabled={loading}>
            {loading ? 'Sending…' : 'Send reset link'}
          </button>
        </form>
      )}
    </AuthShell>
  );
}
