import { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import { api } from '../api/client';
import type { Alert, Company, CompanySummary, CompanyUser, Device, Reading } from '../api/types';
import { getDeviceStatus, summarizeDeviceStatuses, type DeviceStatus } from '../utils/monitoring';
import { useMonitoringClock } from './useMonitoringClock';
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

type DeviceRow = {
  device: Device;
  companyId: string;
  companyName: string;
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

  return {
    company,
    devices: companyDevices.map((device) => ({
      device,
      companyId: company.id,
      companyName: company.name,
    })),
    readings,
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
    activeAlerts: alertsRes.data?.length ?? 0,
  };
}

const deviceToneOrder = { danger: 0, warning: 1, muted: 2, ok: 3 };

export function useGlobalMonitoring(token: string | null) {
  const now = useMonitoringClock();
  const [companies, setCompanies] = useState<Company[]>([]);
  const [deviceRows, setDeviceRows] = useState<DeviceRow[]>([]);
  const [readingsByCompanyId, setReadingsByCompanyId] = useState<Record<string, Reading[]>>({});
  const [alerts, setAlerts] = useState<GlobalAlert[]>([]);
  const [alertHistory, setAlertHistory] = useState<GlobalAlert[]>([]);
  const [membersByCompanyId, setMembersByCompanyId] = useState<Record<string, CompanyUser[]>>({});
  const [activeAlertsByCompanyId, setActiveAlertsByCompanyId] = useState<Record<string, number>>({});
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const refreshTimerRef = useRef<number | null>(null);

  const refresh = useCallback(async () => {
    if (!token) return;
    setLoading(true);
    setError(null);

    const companiesRes = await api.getCompanies(token);
    if (!companiesRes.success || !companiesRes.data) {
      setCompanies([]);
      setDeviceRows([]);
      setReadingsByCompanyId({});
      setAlerts([]);
      setAlertHistory([]);
      setMembersByCompanyId({});
      setActiveAlertsByCompanyId({});
      setLoading(false);
      setError(companiesRes.message ?? 'Failed to load monitoring data');
      return;
    }

    const enriched = await Promise.all(
      companiesRes.data.map(async (company) => loadCompanyMonitoring(token, company)),
    );

    setCompanies(enriched.map((item) => item.company));
    setDeviceRows(enriched.flatMap((item) => item.devices));
    setReadingsByCompanyId(
      Object.fromEntries(enriched.map((item) => [item.company.id, item.readings])),
    );
    setMembersByCompanyId(
      Object.fromEntries(enriched.map((item) => [item.company.id, item.members])),
    );
    setActiveAlertsByCompanyId(
      Object.fromEntries(enriched.map((item) => [item.company.id, item.activeAlerts])),
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

  const summaries = useMemo(
    () =>
      companies.map((company) => {
        const companyDevices = deviceRows
          .filter((row) => row.companyId === company.id)
          .map((row) => row.device);
        const readings = readingsByCompanyId[company.id] ?? [];
        const statusCounts = summarizeDeviceStatuses(companyDevices, readings, now);

        return {
          company,
          deviceCount: companyDevices.length,
          activeAlerts: activeAlertsByCompanyId[company.id] ?? 0,
          ...statusCounts,
        } satisfies CompanySummary;
      }),
    [companies, deviceRows, readingsByCompanyId, activeAlertsByCompanyId, now],
  );

  const devices = useMemo(
    () =>
      deviceRows
        .map((row) => ({
          ...row,
          status: getDeviceStatus(row.device, readingsByCompanyId[row.companyId] ?? [], now),
        }))
        .sort((a, b) => {
          const toneDiff = deviceToneOrder[a.status.tone] - deviceToneOrder[b.status.tone];
          if (toneDiff !== 0) return toneDiff;
          return a.device.name.localeCompare(b.device.name);
        }),
    [deviceRows, readingsByCompanyId, now],
  );

  const totals = useMemo(
    () =>
      summaries.reduce(
        (acc, item) => ({
          companies: acc.companies + 1,
          devices: acc.devices + item.deviceCount,
          alerts: acc.alerts + item.activeAlerts,
          devicesOffline: acc.devicesOffline + item.devicesOffline,
        }),
        { companies: 0, devices: 0, alerts: 0, devicesOffline: 0 },
      ),
    [summaries],
  );

  return { summaries, devices, alerts, alertHistory, membersByCompanyId, loading, error, refresh, totals };
}
