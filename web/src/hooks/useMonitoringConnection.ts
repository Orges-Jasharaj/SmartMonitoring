import { useEffect, useState } from 'react';
import { useAuth } from '../auth/AuthContext';
import {
  connectMonitoringHub,
  disconnectMonitoringHub,
  isMonitoringHubConnected,
  onMonitoringConnectionChange,
} from '../realtime/monitoringHub';

export function useMonitoringConnection() {
  const { token, isAuthenticated } = useAuth();
  const [connected, setConnected] = useState(isMonitoringHubConnected());

  useEffect(() => {
    if (!isAuthenticated || !token) {
      void disconnectMonitoringHub();
      return;
    }

    void connectMonitoringHub(token);
    const unsubscribe = onMonitoringConnectionChange(setConnected);
    return () => {
      unsubscribe();
    };
  }, [isAuthenticated, token]);

  return connected;
}
