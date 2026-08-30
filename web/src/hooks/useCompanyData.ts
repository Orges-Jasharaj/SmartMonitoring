import { useCallback, useEffect, useState } from 'react';
import { api } from '../api/client';
import type { Alert, Company, CompanyUser, Device, Reading, User } from '../api/types';
import { useMonitoringHub } from './useMonitoringHub';
import {
  mergeAlertState,
  prependReading,
  updateDeviceLastReading,
} from '../realtime/monitoringHub';
import { summarizeDeviceStatuses } from '../utils/monitoring';
import { useMonitoringClock } from './useMonitoringClock';

type CompanyData = {
  company: Company | null;
  devices: Device[];
  alerts: Alert[];
  alertHistory: Alert[];
  readings: Reading[];
  members: CompanyUser[];
  users: User[];
  loading: boolean;
  error: string | null;
  lastUpdated: Date | null;
};

const initialState: CompanyData = {
  company: null,
  devices: [],
  alerts: [],
  alertHistory: [],
  readings: [],
  members: [],
  users: [],
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

    const [companyRes, devicesRes, alertsRes, historyRes, readingsRes, membersRes, usersRes] =
      await Promise.all([
        api.getCompany(token, companyId),
        api.getDevices(token, companyId),
        api.getAlerts(token, companyId, true),
        api.getAlerts(token, companyId, false),
        api.getReadings(token, companyId, undefined, 100),
        api.getCompanyUsers(token, companyId),
        api.getUsers(token),
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
      devices: devicesRes.data ?? [],
      alerts: alertsRes.data ?? [],
      alertHistory: historyRes.data ?? [],
      readings: readingsRes.data ?? [],
      members: membersRes.data ?? [],
      users: usersRes.data ?? [],
      loading: false,
      error: null,
      lastUpdated: new Date(),
    });
  }, [token, companyId]);

  useEffect(() => {
    void refresh();
  }, [refresh]);

  const handleReading = useCallback(
    (reading: Reading) => {
      if (reading.companyId !== companyId) return;
      setData((current) => ({
        ...current,
        readings: prependReading(current.readings, reading),
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
        const nextAlerts = mergeAlertState(current.alerts, current.alertHistory, alert);
        return {
          ...current,
          ...nextAlerts,
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

  const statusCounts = summarizeDeviceStatuses(data.devices, data.readings, now);

  return {
    ...data,
    refresh,
    stats: {
      deviceCount: data.devices.length,
      activeAlerts: data.alerts.length,
      ...statusCounts,
      memberCount: data.members.length,
      readingCount: data.readings.length,
    },
  };
}
