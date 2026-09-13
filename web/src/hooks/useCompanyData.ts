import { useCallback, useEffect, useState } from 'react';
import { api } from '../api/client';
import type { Alert, Company, CompanyUser, Device } from '../api/types';
import { useMonitoringHub } from './useMonitoringHub';
import {
  mergeAlertState,
  updateDeviceLastReading,
} from '../realtime/monitoringHub';
import { summarizeDeviceStatusesFromSnapshots } from '../utils/monitoring';
import { useMonitoringClock } from './useMonitoringClock';

type CompanyData = {
  company: Company | null;
  devices: Device[];
  deviceTotalCount: number;
  alerts: Alert[];
  alertTotalCount: number;
  members: CompanyUser[];
  loading: boolean;
  error: string | null;
  lastUpdated: Date | null;
};

const initialState: CompanyData = {
  company: null,
  devices: [],
  deviceTotalCount: 0,
  alerts: [],
  alertTotalCount: 0,
  members: [],
  loading: true,
  error: null,
  lastUpdated: null,
};

export function useCompanyData(companyId: string, token: string | null) {
  const now = useMonitoringClock();
  const [data, setData] = useState<CompanyData>(initialState);

  const refresh = useCallback(async () => {
    if (!token || !companyId) return;

    setData((current) => ({ ...current, loading: current.company === null, error: null }));

    const [companyRes, devicesRes, alertsRes, membersRes] = await Promise.all([
      api.getCompany(token, companyId),
      api.getDevices(token, companyId, { page: 1, pageSize: 200 }),
      api.getAlerts(token, companyId, { activeOnly: true, page: 1, pageSize: 200 }),
      api.getCompanyUsers(token, companyId),
    ]);

    if (!companyRes.success) {
      setData((current) => ({
        ...current,
        loading: false,
        error: companyRes.message ?? 'Failed to load company',
      }));
      return;
    }

    setData({
      company: companyRes.data ?? null,
      devices: devicesRes.data?.items ?? [],
      deviceTotalCount: devicesRes.data?.totalCount ?? 0,
      alerts: alertsRes.data?.items ?? [],
      alertTotalCount: alertsRes.data?.totalCount ?? 0,
      members: membersRes.data ?? [],
      loading: false,
      error: null,
      lastUpdated: new Date(),
    });
  }, [token, companyId]);

  useEffect(() => {
    void refresh();
  }, [refresh]);

  const handleReading = useCallback(
    (reading: { companyId: string; deviceId: string; measuredAtUtc: string }) => {
      if (reading.companyId !== companyId) return;
      setData((current) => ({
        ...current,
        devices: updateDeviceLastReading(current.devices, reading.deviceId, reading.measuredAtUtc),
        lastUpdated: new Date(),
      }));
    },
    [companyId],
  );

  const handleAlert = useCallback(
    (alert: Alert) => {
      if (alert.companyId !== companyId) return;
      setData((current) => {
        const nextAlerts = mergeAlertState(current.alerts, [], alert);
        return {
          ...current,
          alerts: nextAlerts.alerts,
          alertTotalCount: alert.isActive
            ? Math.max(current.alertTotalCount, nextAlerts.alerts.length)
            : current.alertTotalCount,
          lastUpdated: new Date(),
        };
      });
    },
    [companyId],
  );

  useMonitoringHub({
    companyId,
    onReading: handleReading,
    onAlert: handleAlert,
  });

  const statusCounts = summarizeDeviceStatusesFromSnapshots(data.devices, data.alerts, now);

  return {
    ...data,
    refresh,
    stats: {
      deviceCount: data.deviceTotalCount,
      activeAlerts: data.alertTotalCount,
      ...statusCounts,
      memberCount: data.members.length,
    },
  };
}
