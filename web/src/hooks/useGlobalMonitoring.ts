import { useCallback, useEffect, useRef, useState } from 'react';
import { api } from '../api/client';
import type { Alert, Company, CompanySummary, CompanyUser, Device } from '../api/types';
import { getDeviceStatus, type DeviceStatus } from '../utils/monitoring';
import { useMonitoringHub } from './useMonitoringHub';

export type GlobalDevice = {
  device: Device;
  companyId: string;
  companyName: string;
  status: DeviceStatus;
};

export type GlobalAlert = {
  alert: Alert;
  companyId: string;
  companyName: string;
  deviceName: string;
};

async function loadCompanyMonitoring(token: string, company: Company) {
  const [devicesRes, alertsRes, alertHistoryRes, readingsRes, membersRes] = await Promise.all([
    api.getDevices(token, company.id),
    api.getAlerts(token, company.id, true),
    api.getAlerts(token, company.id, false),
    api.getReadings(token, company.id, undefined, 50),
    api.getCompanyUsers(token, company.id),
  ]);

  const companyDevices = devicesRes.data ?? [];
  const readings = readingsRes.data ?? [];
  const deviceNames = new Map(companyDevices.map((device) => [device.id, device.name]));

  const devicesOk = companyDevices.filter((d) => getDeviceStatus(d, readings).tone === 'ok').length;
  const devicesAlerting = companyDevices.filter((d) => getDeviceStatus(d, readings).tone === 'danger').length;

  return {
    summary: {
      company,
      deviceCount: companyDevices.length,
      activeAlerts: alertsRes.data?.length ?? 0,
      devicesOk,
      devicesAlerting,
    } satisfies CompanySummary,
    devices: companyDevices.map((device) => ({
      device,
      companyId: company.id,
      companyName: company.name,
      status: getDeviceStatus(device, readings),
    })),
    alerts: (alertsRes.data ?? []).map((alert) => ({
      alert,
      companyId: company.id,
      companyName: company.name,
      deviceName: deviceNames.get(alert.deviceId) ?? alert.deviceId.slice(0, 8),
    })),
    alertHistory: (alertHistoryRes.data ?? []).map((alert) => ({
      alert,
      companyId: company.id,
      companyName: company.name,
      deviceName: deviceNames.get(alert.deviceId) ?? alert.deviceId.slice(0, 8),
    })),
    members: membersRes.data ?? [],
  };
}

const deviceToneOrder = { danger: 0, warning: 1, muted: 2, ok: 3 };

export function useGlobalMonitoring(token: string | null) {
  const [summaries, setSummaries] = useState<CompanySummary[]>([]);
  const [devices, setDevices] = useState<GlobalDevice[]>([]);
  const [alerts, setAlerts] = useState<GlobalAlert[]>([]);
  const [alertHistory, setAlertHistory] = useState<GlobalAlert[]>([]);
  const [membersByCompanyId, setMembersByCompanyId] = useState<Record<string, CompanyUser[]>>({});
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const refreshTimerRef = useRef<number | null>(null);

  const refresh = useCallback(async () => {
    if (!token) return;
    setLoading(true);
    setError(null);

    const companiesRes = await api.getCompanies(token);
    if (!companiesRes.success || !companiesRes.data) {
      setSummaries([]);
      setDevices([]);
      setAlerts([]);
      setAlertHistory([]);
      setMembersByCompanyId({});
      setLoading(false);
      setError(companiesRes.message ?? 'Failed to load monitoring data');
      return;
    }

    const enriched = await Promise.all(
      companiesRes.data.map(async (company) => loadCompanyMonitoring(token, company)),
    );

    setSummaries(enriched.map((item) => item.summary));
    setMembersByCompanyId(
      Object.fromEntries(enriched.map((item) => [item.summary.company.id, item.members])),
    );
    setDevices(
      enriched
        .flatMap((item) => item.devices)
        .sort((a, b) => {
          const toneDiff = deviceToneOrder[a.status.tone] - deviceToneOrder[b.status.tone];
          if (toneDiff !== 0) return toneDiff;
          return a.device.name.localeCompare(b.device.name);
        }),
    );
    setAlerts(
      enriched
        .flatMap((item) => item.alerts)
        .sort(
          (a, b) =>
            new Date(b.alert.triggeredAtUtc).getTime() - new Date(a.alert.triggeredAtUtc).getTime(),
        ),
    );
    setAlertHistory(
      enriched
        .flatMap((item) => item.alertHistory)
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
    }, 400);
  }, [refresh]);

  useEffect(() => {
    void refresh();
  }, [refresh]);

  useMonitoringHub({
    onReading: scheduleRefresh,
    onAlert: scheduleRefresh,
  });

  useEffect(
    () => () => {
      if (refreshTimerRef.current) {
        window.clearTimeout(refreshTimerRef.current);
      }
    },
    [],
  );

  const totals = summaries.reduce(
    (acc, item) => ({
      companies: acc.companies + 1,
      devices: acc.devices + item.deviceCount,
      alerts: acc.alerts + item.activeAlerts,
    }),
    { companies: 0, devices: 0, alerts: 0 },
  );

  return { summaries, devices, alerts, alertHistory, membersByCompanyId, loading, error, refresh, totals };
}
