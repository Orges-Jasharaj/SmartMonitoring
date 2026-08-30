import { useEffect, useRef, useState } from 'react';
import { Link } from 'react-router-dom';
import { useAuth } from '../auth/AuthContext';
import { AlertAcknowledgeButton } from './AlertAcknowledgeButton';
import { useToast } from './Toast';
import { useAlertNotifications } from '../hooks/useAlertNotifications';
import { formatDateTime } from '../utils/monitoring';

function BellIcon() {
  return (
    <svg className="notification-icon" viewBox="0 0 24 24" aria-hidden="true">
      <path
        d="M12 2a5 5 0 0 0-5 5v2.1c0 .5-.2 1-.5 1.4L5.1 12.8A1 1 0 0 0 6 14.5h12a1 1 0 0 0 .9-1.4l-1.4-2.3c-.3-.4-.5-.9-.5-1.4V7a5 5 0 0 0-5-5Zm0 20a2.5 2.5 0 0 0 2.45-2h-4.9A2.5 2.5 0 0 0 12 22Z"
        fill="currentColor"
      />
    </svg>
  );
}

export function AlertNotificationBell() {
  const { token, userId } = useAuth();
  const { pushToast } = useToast();
  const [open, setOpen] = useState(false);
  const { items, loading, refresh, unreadCount } = useAlertNotifications(token, userId, open);
  const rootRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    if (!open) return;

    function handleClickOutside(event: MouseEvent) {
      if (rootRef.current && !rootRef.current.contains(event.target as Node)) {
        setOpen(false);
      }
    }

    function handleEscape(event: KeyboardEvent) {
      if (event.key === 'Escape') {
        setOpen(false);
      }
    }

    document.addEventListener('mousedown', handleClickOutside);
    document.addEventListener('keydown', handleEscape);
    return () => {
      document.removeEventListener('mousedown', handleClickOutside);
      document.removeEventListener('keydown', handleEscape);
    };
  }, [open]);

  function toggleOpen() {
    const next = !open;
    setOpen(next);
    if (next) {
      void refresh();
    }
  }

  return (
    <>
      {open && <button type="button" className="notification-backdrop" aria-label="Close alerts" onClick={() => setOpen(false)} />}

      <div className="notification-root" ref={rootRef}>
        <button
          type="button"
          className="notification-btn"
          onClick={toggleOpen}
          aria-label={`Alerts${unreadCount > 0 ? `, ${unreadCount} unread` : ''}`}
          aria-expanded={open}
        >
          <BellIcon />
          {unreadCount > 0 && (
            <span className="notification-badge">{unreadCount > 99 ? '99+' : unreadCount}</span>
          )}
        </button>

        {open && (
          <div className="notification-panel">
            <div className="notification-panel-header">
              <strong>Active alerts</strong>
              <button type="button" className="btn btn-ghost btn-sm" onClick={() => void refresh()}>
                Refresh
              </button>
            </div>

            {loading && items.length === 0 && <p className="muted small notification-empty">Loading…</p>}

            {!loading && items.length === 0 && (
              <p className="muted small notification-empty">No active alerts right now.</p>
            )}

            <ul className="notification-list">
              {items.map(({ alert, companyId, companyName, deviceName }) => (
                <li key={alert.id} className="notification-list-item">
                  <div className="alert-item-row">
                    <Link
                      to={`/companies/${companyId}?tab=alerts`}
                      className="notification-item alert-item-body"
                      onClick={() => setOpen(false)}
                    >
                      <div className="notification-item-top">
                        <strong>{deviceName}</strong>
                        <span className="pill pill-danger">Alert</span>
                      </div>
                      <p className="muted small">{companyName}</p>
                      <p className="small">{alert.message}</p>
                      {alert.temperatureC != null && (
                        <p className="small danger-text">{alert.temperatureC}°C</p>
                      )}
                      <p className="muted small">{formatDateTime(alert.triggeredAtUtc)}</p>
                    </Link>
                    <AlertAcknowledgeButton
                      token={token}
                      companyId={companyId}
                      alert={alert}
                      onAcknowledged={() => {
                        pushToast('Alert acknowledged', 'success');
                        void refresh();
                      }}
                      onError={(message) => pushToast(message, 'error')}
                    />
                  </div>
                </li>
              ))}
            </ul>
          </div>
        )}
      </div>
    </>
  );
}
