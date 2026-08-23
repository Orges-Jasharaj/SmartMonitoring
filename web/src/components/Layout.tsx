import { NavLink, Outlet } from 'react-router-dom';
import { useAuth } from '../auth/AuthContext';

export function Layout() {
  const { logout, userName, isAdmin } = useAuth();
  const initials = (userName ?? '?').slice(0, 2).toUpperCase();

  return (
    <div className="app-shell">
      <header className="topbar">
        <NavLink to="/" className="brand brand-link">
          <span className="brand-mark" aria-hidden="true" />
          <div>
            <strong>SmartMonitoring</strong>
            <span>Temperature monitoring</span>
          </div>
        </NavLink>

        <nav className="topnav" aria-label="Main">
          <NavLink to="/" end>
            Dashboard
          </NavLink>
          {isAdmin && (
            <>
              <NavLink to="/audit">Audit</NavLink>
              <NavLink to="/admin/users">Users</NavLink>
              <NavLink to="/admin/roles">Roles</NavLink>
            </>
          )}
          <NavLink to="/profile">Profile</NavLink>
        </nav>

        <div className="topbar-user">
          <div className="user-pill" title={userName ?? undefined}>
            <span className="user-avatar">{initials}</span>
            {userName && <span className="user-name">{userName}</span>}
          </div>
          <button type="button" className="btn btn-ghost btn-sm" onClick={logout}>
            Sign out
          </button>
        </div>
      </header>
      <main className="page">
        <Outlet />
      </main>
    </div>
  );
}
