import type { CompanyUser, Device, Reading } from '../api/types';

export const DEVICE_OFFLINE_AFTER_MS = 30 * 60 * 1000;

export type DeviceStatusTone = 'ok' | 'danger' | 'muted' | 'warning';

export type DeviceStatus = {
  label: string;
  tone: DeviceStatusTone;
  latestTemp?: number;
};

export function getLatestReading(readings: Reading[], deviceId: string): Reading | undefined {
  return readings
    .filter((r) => r.deviceId === deviceId)
    .sort((a, b) => new Date(b.measuredAtUtc).getTime() - new Date(a.measuredAtUtc).getTime())[0];
}

export function getDeviceLastReadingAt(device: Device, readings: Reading[]): Date | null {
  const latest = getLatestReading(readings, device.id);
  const timestamps = [latest?.measuredAtUtc, device.lastReadingAtUtc].filter(Boolean) as string[];

  if (timestamps.length === 0) {
    return null;
  }

  return new Date(
    Math.max(...timestamps.map((value) => new Date(value).getTime())),
  );
}

export function isDeviceOffline(lastReadingAt: Date | null, now = new Date()) {
  if (!lastReadingAt) {
    return false;
  }

  return now.getTime() - lastReadingAt.getTime() > DEVICE_OFFLINE_AFTER_MS;
}

export function getDeviceStatus(device: Device, readings: Reading[], now = new Date()): DeviceStatus {
  const latest = getLatestReading(readings, device.id);
  const lastReadingAt = getDeviceLastReadingAt(device, readings);

  if (!lastReadingAt) {
    return { label: 'No data', tone: 'muted' };
  }

  if (isDeviceOffline(lastReadingAt, now)) {
    return {
      label: 'Offline',
      tone: 'warning',
      latestTemp: latest?.temperatureC,
    };
  }

  if (!latest) {
    return { label: 'No data', tone: 'muted' };
  }

  if (latest.temperatureC < device.minTempC || latest.temperatureC > device.maxTempC) {
    return { label: 'Out of range', tone: 'danger', latestTemp: latest.temperatureC };
  }

  return { label: 'OK', tone: 'ok', latestTemp: latest.temperatureC };
}

export function summarizeDeviceStatuses(devices: Device[], readings: Reading[], now = new Date()) {
  return devices.reduce(
    (acc, device) => {
      const tone = getDeviceStatus(device, readings, now).tone;
      if (tone === 'ok') {
        acc.devicesOk += 1;
      } else if (tone === 'danger') {
        acc.devicesAlerting += 1;
      } else if (tone === 'warning') {
        acc.devicesOffline += 1;
      }
      return acc;
    },
    { devicesOk: 0, devicesAlerting: 0, devicesOffline: 0 },
  );
}

export function deviceStatusRowClass(tone: DeviceStatusTone) {
  if (tone === 'danger') {
    return ' status-row-alert';
  }

  if (tone === 'warning') {
    return ' status-row-offline';
  }

  return '';
}

export function deviceCardClass(tone: DeviceStatusTone) {
  if (tone === 'danger') {
    return ' device-card-alert';
  }

  if (tone === 'warning') {
    return ' device-card-offline';
  }

  return '';
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
  return members.some(
    (member) =>
      member.userId.toLowerCase() === normalizedUserId && member.role === 'CompanyAdmin',
  );
}
