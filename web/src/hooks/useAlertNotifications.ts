import { useCallback, useEffect, useRef, useState } from 'react';
import { api } from '../api/client';
import type { Alert } from '../api/types';
import { useMonitoringHub } from './useMonitoringHub';

export type AlertNotificationItem = {
  alert: Alert;
  companyId: string;
  companyName: string;
  deviceName: string;
};

export function useAlertNotifications(token: string | null) {
  const [items, setItems] = useState<AlertNotificationItem[]>([]);
  const [loading, setLoading] = useState(false);
  const refreshTimerRef = useRef<number | null>(null);

  const refresh = useCallback(async () => {
    if (!token) {
      setItems([]);
      return;
    }

    setLoading(true);
    const companiesRes = await api.getCompanies(token);
    if (!companiesRes.success || !companiesRes.data) {
      setItems([]);
      setLoading(false);
      return;
    }

    const nested = await Promise.all(
      companiesRes.data.map(async (company) => {
        const [alertsRes, devicesRes] = await Promise.all([
          api.getAlerts(token, company.id, true),
          api.getDevices(token, company.id),
        ]);

        const deviceNames = new Map((devicesRes.data ?? []).map((device) => [device.id, device.name]));

        return (alertsRes.data ?? []).map((alert) => ({
          alert,
          companyId: company.id,
          companyName: company.name,
          deviceName: deviceNames.get(alert.deviceId) ?? alert.deviceId.slice(0, 8),
        }));
      }),
    );

    setItems(
      nested
        .flat()
        .sort(
          (a, b) =>
            new Date(b.alert.triggeredAtUtc).getTime() - new Date(a.alert.triggeredAtUtc).getTime(),
        ),
    );
    setLoading(false);
  }, [token]);

  const scheduleRefresh = useCallback(() => {
    if (refreshTimerRef.current) {
      window.clearTimeout(refreshTimerRef.current);
    }

    refreshTimerRef.current = window.setTimeout(() => {
      void refresh();
    }, 350);
  }, [refresh]);

  useEffect(() => {
    void refresh();
  }, [refresh]);

  useMonitoringHub({ onAlert: scheduleRefresh });

  useEffect(
    () => () => {
      if (refreshTimerRef.current) {
        window.clearTimeout(refreshTimerRef.current);
      }
    },
    [],
  );

  return { items, loading, refresh, count: items.length };
}
