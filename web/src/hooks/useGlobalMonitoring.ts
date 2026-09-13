import { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import { api } from '../api/client';
import type { Alert, Company, CompanySummary, CompanyUser, Device } from '../api/types';
import {
  getDeviceStatusFromSnapshot,
  parseUtcDateTime,
  summarizeDeviceStatusesFromSnapshots,
  type DeviceStatus,
} from '../utils/monitoring';
import { useMonitoringClock } from './useMonitoringClock';
import { updateDeviceLastReading } from '../realtime/monitoringHub';
import { useMonitoringHub } from './useMonitoringHub';

export type GlobalDevice = {
  device: Device;
  companyId: string;
  companyName: string;
  status: DeviceStatus;
  lastReadingAt: Date | null;
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

const GLOBAL_FETCH_PAGE_SIZE = 200;

async function loadCompanyMonitoring(token: string, company: Company) {
  const [devicesRes, alertsRes, membersRes] = await Promise.all([
    api.getDevices(token, company.id, { page: 1, pageSize: GLOBAL_FETCH_PAGE_SIZE }),
    api.getAlerts(token, company.id, { activeOnly: true, page: 1, pageSize: GLOBAL_FETCH_PAGE_SIZE }),
    api.getCompanyUsers(token, company.id),
  ]);

  const companyDevices = devicesRes.data?.items ?? [];
  const activeAlerts = alertsRes.data?.items ?? [];
  const deviceNames = new Map(companyDevices.map((device) => [device.id, device.name]));

  return {
    company,
    devices: companyDevices.map((device) => ({
      device,
      companyId: company.id,
      companyName: company.name,
    })),
    alerts: activeAlerts.map((alert) => ({
      alert,
      companyId: company.id,
      companyName: company.name,
      deviceName: deviceNames.get(alert.deviceId) ?? alert.deviceId.slice(0, 8),
    })),
    members: membersRes.data ?? [],
    activeAlerts: alertsRes.data?.totalCount ?? activeAlerts.length,
    deviceCount: devicesRes.data?.totalCount ?? companyDevices.length,
  };
}

const deviceToneOrder = { danger: 0, warning: 1, muted: 2, ok: 3 };

export function useGlobalMonitoring(token: string | null) {
  const now = useMonitoringClock();
  const [companies, setCompanies] = useState<Company[]>([]);
  const [deviceRows, setDeviceRows] = useState<DeviceRow[]>([]);
  const [alertsByCompanyId, setAlertsByCompanyId] = useState<Record<string, Alert[]>>({});
  const [alerts, setAlerts] = useState<GlobalAlert[]>([]);
  const [membersByCompanyId, setMembersByCompanyId] = useState<Record<string, CompanyUser[]>>({});
  const [activeAlertsByCompanyId, setActiveAlertsByCompanyId] = useState<Record<string, number>>({});
  const [deviceCountByCompanyId, setDeviceCountByCompanyId] = useState<Record<string, number>>({});
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
      setAlerts([]);
      setAlertsByCompanyId({});
      setMembersByCompanyId({});
      setActiveAlertsByCompanyId({});
      setDeviceCountByCompanyId({});
      setLoading(false);
      setError(companiesRes.message ?? 'Failed to load monitoring data');
      return;
    }

    const enriched = await Promise.all(
      companiesRes.data.map(async (company) => loadCompanyMonitoring(token, company)),
    );

    setCompanies(enriched.map((item) => item.company));
    setDeviceRows(enriched.flatMap((item) => item.devices));
    setAlertsByCompanyId(
      Object.fromEntries(enriched.map((item) => [item.company.id, item.alerts.map((entry) => entry.alert)])),
    );
    setMembersByCompanyId(
      Object.fromEntries(enriched.map((item) => [item.company.id, item.members])),
    );
    setActiveAlertsByCompanyId(
      Object.fromEntries(enriched.map((item) => [item.company.id, item.activeAlerts])),
    );
    setDeviceCountByCompanyId(
      Object.fromEntries(enriched.map((item) => [item.company.id, item.deviceCount])),
    );
    setAlerts(
      enriched
        .flatMap((item) => item.alerts)
        .sort(
          (a, b) =>
            (parseUtcDateTime(b.alert.triggeredAtUtc)?.getTime() ?? 0) -
            (parseUtcDateTime(a.alert.triggeredAtUtc)?.getTime() ?? 0),
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

  const handleReading = useCallback(
    (reading: { companyId: string; deviceId: string; measuredAtUtc: string }) => {
      setDeviceRows((current) =>
        current.map((row) =>
          row.device.id.toLowerCase() === reading.deviceId.toLowerCase()
            ? {
                ...row,
                device: updateDeviceLastReading([row.device], reading.deviceId, reading.measuredAtUtc)[0],
              }
            : row,
        ),
      );
      scheduleRefresh();
    },
    [scheduleRefresh],
  );

  useEffect(() => {
    void refresh();
  }, [refresh]);

  useEffect(() => {
    if (!token) return;
    const timer = window.setInterval(() => {
      void refresh();
    }, 30_000);
    return () => window.clearInterval(timer);
  }, [token, refresh]);

  useMonitoringHub({
    onReading: handleReading,
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
        const companyAlerts = alertsByCompanyId[company.id] ?? [];
        const statusCounts = summarizeDeviceStatusesFromSnapshots(companyDevices, companyAlerts, now);

        return {
          company,
          deviceCount: deviceCountByCompanyId[company.id] ?? companyDevices.length,
          activeAlerts: activeAlertsByCompanyId[company.id] ?? 0,
          ...statusCounts,
        } satisfies CompanySummary;
      }),
    [companies, deviceRows, alertsByCompanyId, activeAlertsByCompanyId, deviceCountByCompanyId, now],
  );

  const devices = useMemo(
    () =>
      deviceRows
        .map((row) => {
          const companyAlerts = alertsByCompanyId[row.companyId] ?? [];
          const deviceAlerts = companyAlerts.filter(
            (alert) => alert.deviceId.toLowerCase() === row.device.id.toLowerCase(),
          );
          const status = getDeviceStatusFromSnapshot(row.device, deviceAlerts, now);
          return {
            ...row,
            status,
            lastReadingAt: status.lastReadingAt ?? null,
          };
        })
        .sort((a, b) => {
          const toneDiff = deviceToneOrder[a.status.tone] - deviceToneOrder[b.status.tone];
          if (toneDiff !== 0) return toneDiff;
          return a.device.name.localeCompare(b.device.name);
        }),
    [deviceRows, alertsByCompanyId, now],
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

  return { summaries, devices, alerts, membersByCompanyId, loading, error, refresh, totals };
}
