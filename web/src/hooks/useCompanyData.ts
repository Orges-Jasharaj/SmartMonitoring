import { useCallback, useEffect, useState } from 'react';
import { api } from '../api/client';
import type { Alert, Company, CompanyUser, Device, Reading, User } from '../api/types';
import { getDeviceStatus } from '../utils/monitoring';

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

  const devicesOk = data.devices.filter(
    (device) => getDeviceStatus(device, data.readings).tone === 'ok',
  ).length;

  const devicesAlerting = data.devices.filter(
    (device) => getDeviceStatus(device, data.readings).tone === 'danger',
  ).length;

  return {
    ...data,
    refresh,
    stats: {
      deviceCount: data.devices.length,
      activeAlerts: data.alerts.length,
      devicesOk,
      devicesAlerting,
      memberCount: data.members.length,
      readingCount: data.readings.length,
    },
  };
}
