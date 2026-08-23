import { NavLink, Outlet } from 'react-router-dom';
import { useAuth } from '../auth/AuthContext';

export function Layout() {
  const { logout } = useAuth();

  return (
    <div className="app-shell">
      <header className="topbar">
        <div className="brand">
          <span className="brand-mark" aria-hidden="true" />
          <div>
            <strong>SmartMonitoring</strong>
            <span>Temperature monitoring</span>
          </div>
        </div>
        <nav className="topnav">
          <NavLink to="/" end>
            Companies
          </NavLink>
        </nav>
        <button type="button" className="btn btn-ghost" onClick={logout}>
          Sign out
        </button>
      </header>
      <main className="page">
        <Outlet />
      </main>
    </div>
  );
}
