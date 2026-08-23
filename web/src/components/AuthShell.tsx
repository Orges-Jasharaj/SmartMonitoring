import type { ReactNode } from 'react';
import { Link } from 'react-router-dom';

type AuthShellProps = {
  title: string;
  subtitle: string;
  children: ReactNode;
  footer?: ReactNode;
  wide?: boolean;
};

export function AuthShell({ title, subtitle, children, footer, wide }: AuthShellProps) {
  return (
    <div className="auth-page">
      <aside className="auth-hero" aria-hidden="true">
        <div className="auth-hero-inner">
          <span className="brand-mark lg" />
          <h1>SmartMonitoring</h1>
          <p>Real-time temperature monitoring for pharmacies, cold storage, and food safety teams.</p>
          <ul className="auth-hero-list">
            <li>Multi-tenant company dashboards</li>
            <li>Device alerts and email notifications</li>
            <li>Full audit trail for compliance</li>
          </ul>
        </div>
      </aside>

      <div className="auth-panel">
        <div className={`auth-card card stack ${wide ? 'auth-card-wide' : ''}`}>
          <div className="auth-card-head">
            <span className="brand-mark sm" aria-hidden="true" />
            <div>
              <strong>SmartMonitoring</strong>
              <span className="muted small">{subtitle}</span>
            </div>
          </div>
          <h2 className="auth-title">{title}</h2>
          {children}
          {footer && <div className="auth-footer">{footer}</div>}
        </div>
      </div>
    </div>
  );
}

export function AuthFooterLink({ to, children }: { to: string; children: ReactNode }) {
  return (
    <Link to={to} className="auth-inline-link">
      {children}
    </Link>
  );
}
