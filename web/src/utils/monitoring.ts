import type { CompanyUser, Device, Reading } from '../api/types';

export const DEVICE_OFFLINE_AFTER_MS = 30 * 60 * 1000;

export type DeviceStatusTone = 'ok' | 'danger' | 'muted' | 'warning';

export type DeviceStatus = {
  label: string;
  tone: DeviceStatusTone;
  latestTemp?: number;
  lastReadingAt?: Date | null;
};

/** Backend sends UTC timestamps without a Z suffix; treat bare ISO strings as UTC. */
export function parseUtcDateTime(value?: string | Date | null): Date | null {
  if (!value) return null;
  if (value instanceof Date) {
    return Number.isNaN(value.getTime()) ? null : value;
  }

  const trimmed = value.trim();
  if (!trimmed) return null;

  const normalized =
    /[Zz]$/.test(trimmed) || /[+-]\d{2}:\d{2}$/.test(trimmed) ? trimmed : `${trimmed}Z`;
  const date = new Date(normalized);
  return Number.isNaN(date.getTime()) ? null : date;
}

export function getReadingsForDevice(readings: Reading[], deviceId: string): Reading[] {
  const normalizedDeviceId = deviceId.toLowerCase();
  return readings.filter((reading) => reading.deviceId?.toLowerCase() === normalizedDeviceId);
}

export function getLatestReading(readings: Reading[], deviceId?: string): Reading | undefined {
  const normalizedDeviceId = deviceId?.toLowerCase();
  const scopedReadings = normalizedDeviceId
    ? getReadingsForDevice(readings, normalizedDeviceId)
    : readings;

  return [...scopedReadings].sort(
    (a, b) =>
      (parseUtcDateTime(b.measuredAtUtc)?.getTime() ?? 0) -
      (parseUtcDateTime(a.measuredAtUtc)?.getTime() ?? 0),
  )[0];
}

export function getDeviceLastReadingAt(device: Device, readings: Reading[]): Date | null {
  const deviceReadings = getReadingsForDevice(readings, device.id);
  const times = deviceReadings
    .map((reading) => parseUtcDateTime(reading.measuredAtUtc)?.getTime())
    .filter((value): value is number => value !== undefined);

  const deviceTime = parseUtcDateTime(device.lastReadingAtUtc)?.getTime();
  if (deviceTime !== undefined) {
    times.push(deviceTime);
  }

  if (times.length === 0) {
    return null;
  }

  return new Date(Math.max(...times));
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
    return { label: 'No data', tone: 'muted', lastReadingAt: null };
  }

  if (isDeviceOffline(lastReadingAt, now)) {
    return {
      label: 'Offline',
      tone: 'warning',
      latestTemp: latest?.temperatureC,
      lastReadingAt,
    };
  }

  if (!latest) {
    return { label: 'No data', tone: 'muted', lastReadingAt };
  }

  if (latest.temperatureC < device.minTempC || latest.temperatureC > device.maxTempC) {
    return { label: 'Out of range', tone: 'danger', latestTemp: latest.temperatureC, lastReadingAt };
  }

  return { label: 'OK', tone: 'ok', latestTemp: latest.temperatureC, lastReadingAt };
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

export function formatDateTime(value?: string | Date | null) {
  const date = parseUtcDateTime(value);
  if (!date) return '—';
  return date.toLocaleString();
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

export function formatMetric(
  value: number | null | undefined,
  suffix: string,
  fallback = '—',
) {
  if (value === null || value === undefined) {
    return fallback;
  }

  return `${value}${suffix}`;
}
