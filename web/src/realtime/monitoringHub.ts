import * as signalR from '@microsoft/signalr';
import type { Alert, Reading } from '../api/types';

type Subscriber = {
  companyId?: string;
  deviceId?: string;
  onReading?: (reading: Reading) => void;
  onAlert?: (alert: Alert) => void;
};

let connection: signalR.HubConnection | null = null;
let activeToken: string | null = null;
let subscriberId = 0;
const subscribers = new Map<number, Subscriber>();
const connectionListeners = new Set<(connected: boolean) => void>();

function notifyConnection(connected: boolean) {
  connectionListeners.forEach((listener) => listener(connected));
}

function dispatchReading(reading: Reading) {
  subscribers.forEach((subscriber) => {
    if (subscriber.companyId && subscriber.companyId !== reading.companyId) return;
    if (subscriber.deviceId && subscriber.deviceId !== reading.deviceId) return;
    subscriber.onReading?.(reading);
  });
}

function dispatchAlert(alert: Alert) {
  subscribers.forEach((subscriber) => {
    if (subscriber.companyId && subscriber.companyId !== alert.companyId) return;
    subscriber.onAlert?.(alert);
  });
}

export function isMonitoringHubConnected() {
  return connection?.state === signalR.HubConnectionState.Connected;
}

export function onMonitoringConnectionChange(listener: (connected: boolean) => void) {
  connectionListeners.add(listener);
  listener(isMonitoringHubConnected());
  return () => {
    connectionListeners.delete(listener);
  };
}

export async function connectMonitoringHub(token: string) {
  if (
    connection &&
    activeToken === token &&
    connection.state === signalR.HubConnectionState.Connected
  ) {
    return;
  }

  if (connection) {
    await connection.stop();
  }

  activeToken = token;
  connection = new signalR.HubConnectionBuilder()
    .withUrl('/monitoring/hubs/monitoring', {
      accessTokenFactory: () => token,
    })
    .withAutomaticReconnect()
    .configureLogging(signalR.LogLevel.Warning)
    .build();

  connection.on('ReadingReceived', dispatchReading);
  connection.on('AlertChanged', dispatchAlert);
  connection.onreconnected(() => notifyConnection(true));
  connection.onclose(() => notifyConnection(false));

  await connection.start();
  notifyConnection(true);
}

export async function disconnectMonitoringHub() {
  if (!connection) return;
  await connection.stop();
  connection = null;
  activeToken = null;
  notifyConnection(false);
}

export function subscribeMonitoringHub(subscriber: Subscriber) {
  const id = ++subscriberId;
  subscribers.set(id, subscriber);
  return () => {
    subscribers.delete(id);
  };
}

export function mergeAlertState(
  alerts: Alert[],
  alertHistory: Alert[],
  alert: Alert,
): { alerts: Alert[]; alertHistory: Alert[] } {
  if (alert.isActive) {
    return {
      alerts: [alert, ...alerts.filter((item) => item.id !== alert.id)],
      alertHistory: alertHistory.filter((item) => item.id !== alert.id),
    };
  }

  return {
    alerts: alerts.filter((item) => item.id !== alert.id),
    alertHistory: [alert, ...alertHistory.filter((item) => item.id !== alert.id)],
  };
}

export function prependReading(readings: Reading[], reading: Reading, limit = 100) {
  return [reading, ...readings.filter((item) => item.id !== reading.id)].slice(0, limit);
}

export function updateDeviceLastReading<T extends { id: string; lastReadingAtUtc?: string | null }>(
  devices: T[],
  deviceId: string,
  measuredAtUtc: string,
) {
  return devices.map((device) =>
    device.id === deviceId ? { ...device, lastReadingAtUtc: measuredAtUtc } : device,
  );
}
