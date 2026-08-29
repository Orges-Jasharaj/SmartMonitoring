import type { CompanyUser, Device, Reading } from '../api/types';

export type DeviceStatusTone = 'ok' | 'danger' | 'muted' | 'warning';

export type DeviceStatus = {
  label: string;
  tone: DeviceStatusTone;
  latestTemp?: number;
};

export function getDeviceStatus(device: Device, readings: Reading[]): DeviceStatus {
  const latest = readings
    .filter((r) => r.deviceId === device.id)
    .sort((a, b) => new Date(b.measuredAtUtc).getTime() - new Date(a.measuredAtUtc).getTime())[0];

  if (!latest) {
    return { label: 'No data', tone: 'muted' };
  }

  if (latest.temperatureC < device.minTempC || latest.temperatureC > device.maxTempC) {
    return { label: 'Out of range', tone: 'danger', latestTemp: latest.temperatureC };
  }

  return { label: 'OK', tone: 'ok', latestTemp: latest.temperatureC };
}

export function getLatestReading(readings: Reading[], deviceId: string): Reading | undefined {
  return readings
    .filter((r) => r.deviceId === deviceId)
    .sort((a, b) => new Date(b.measuredAtUtc).getTime() - new Date(a.measuredAtUtc).getTime())[0];
}

export function formatDateTime(value?: string | null) {
  if (!value) return '—';
  return new Date(value).toLocaleString();
}

export async function copyToClipboard(text: string) {
  await navigator.clipboard.writeText(text);
}

export function canManageCompanyDevices(
  isSystemAdmin: boolean,
  userId: string | null,
  members: CompanyUser[],
) {
  if (isSystemAdmin) {
    return true;
  }

  if (!userId) {
    return false;
  }

  const normalizedUserId = userId.toLowerCase();
  return members.some((member) => member.userId.toLowerCase() === normalizedUserId);
}
