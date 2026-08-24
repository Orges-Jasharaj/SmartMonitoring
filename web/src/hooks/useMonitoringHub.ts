import { useEffect } from 'react';
import { useAuth } from '../auth/AuthContext';
import type { Alert, Reading } from '../api/types';
import { connectMonitoringHub, subscribeMonitoringHub } from '../realtime/monitoringHub';

type UseMonitoringHubOptions = {
  companyId?: string;
  deviceId?: string;
  onReading?: (reading: Reading) => void;
  onAlert?: (alert: Alert) => void;
};

export function useMonitoringHub({
  companyId,
  deviceId,
  onReading,
  onAlert,
}: UseMonitoringHubOptions) {
  const { token } = useAuth();

  useEffect(() => {
    if (!token) return;

    void connectMonitoringHub(token);
    const unsubscribe = subscribeMonitoringHub({ companyId, deviceId, onReading, onAlert });
    return () => {
      unsubscribe();
    };
  }, [token, companyId, deviceId, onReading, onAlert]);
}
