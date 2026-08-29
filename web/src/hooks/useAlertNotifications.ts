import { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import { api } from '../api/client';
import type { Alert } from '../api/types';
import { loadSeenAlertIds, saveSeenAlertIds } from '../utils/seenAlerts';
import { useMonitoringHub } from './useMonitoringHub';

export type AlertNotificationItem = {
  alert: Alert;
  companyId: string;
  companyName: string;
  deviceName: string;
};

export function useAlertNotifications(
  token: string | null,
  userId: string | null,
  panelOpen: boolean,
) {
  const [items, setItems] = useState<AlertNotificationItem[]>([]);
  const [loading, setLoading] = useState(() => Boolean(token));
  const [hasLoadedAlerts, setHasLoadedAlerts] = useState(false);
  const [seenAlertIds, setSeenAlertIds] = useState<Set<string>>(() => loadSeenAlertIds(userId));
  const refreshTimerRef = useRef<number | null>(null);

  useEffect(() => {
    setSeenAlertIds(loadSeenAlertIds(userId));
  }, [userId]);

  useEffect(() => {
    setHasLoadedAlerts(false);
  }, [token]);

  const updateSeenAlertIds = useCallback(
    (updater: (current: Set<string>) => Set<string>) => {
      setSeenAlertIds((current) => {
        const next = updater(current);
        saveSeenAlertIds(userId, next);
        return next;
      });
    },
    [userId],
  );

  const refresh = useCallback(async () => {
    if (!token) {
      setItems([]);
      setHasLoadedAlerts(false);
      setLoading(false);
      return;
    }

    setLoading(true);
    const companiesRes = await api.getCompanies(token);
    if (!companiesRes.success || !companiesRes.data) {
      setItems([]);
      setHasLoadedAlerts(true);
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
    setHasLoadedAlerts(true);
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

  // Drop seen IDs for alerts that are no longer active (only after alerts have loaded).
  useEffect(() => {
    if (!hasLoadedAlerts || loading) return;

    const activeIds = new Set(items.map((item) => item.alert.id));
    updateSeenAlertIds((current) => {
      const next = new Set([...current].filter((id) => activeIds.has(id)));
      return next.size === current.size ? current : next;
    });
  }, [items, loading, hasLoadedAlerts, updateSeenAlertIds]);

  // Mark everything in the panel as seen once the user opens it and data is loaded.
  useEffect(() => {
    if (!panelOpen || loading) return;

    updateSeenAlertIds((current) => {
      const next = new Set(current);
      items.forEach((item) => next.add(item.alert.id));
      return next.size === current.size ? current : next;
    });
  }, [panelOpen, loading, items, updateSeenAlertIds]);

  const unreadCount = useMemo(
    () => items.filter((item) => !seenAlertIds.has(item.alert.id)).length,
    [items, seenAlertIds],
  );

  return { items, loading, refresh, unreadCount, totalCount: items.length };
}
