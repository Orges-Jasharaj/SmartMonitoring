export type ApiResponse<T> = {
  success: boolean;
  message?: string | null;
  data?: T;
  errors?: { errorCode?: string; errorMessage?: string }[];
};

export type JwtResult = {
  token: string;
  expiresAt: string;
  refreshToken?: string | null;
  refreshTokenExpiresAt?: string | null;
};

export type Company = {
  id: string;
  name: string;
  isActive: boolean;
  createdAtUtc: string;
};

export type Device = {
  id: string;
  companyId: string;
  name: string;
  zoneName: string;
  minTempC: number;
  maxTempC: number;
  minHumidityPct: number;
  maxHumidityPct: number;
  minCo2Ppm: number;
  maxCo2Ppm: number;
  minLightLevelLux: number;
  maxLightLevelLux: number;
  minNoiseLevelDb: number;
  maxNoiseLevelDb: number;
  minBatteryPct: number;
  isActive: boolean;
  lastReadingAtUtc?: string | null;
  createdAtUtc: string;
};

export type DeviceCreated = Device & {
  deviceKey: string;
};

export type DeviceKey = {
  deviceKey: string;
};

export type Alert = {
  id: string;
  deviceId: string;
  companyId: string;
  alertType: string;
  message: string;
  temperatureC?: number | null;
  triggeredAtUtc: string;
  resolvedAtUtc?: string | null;
  isActive: boolean;
};

export type Reading = {
  id: string;
  deviceId: string;
  companyId: string;
  temperatureC: number;
  humidityPct?: number | null;
  co2Ppm?: number | null;
  lightLevelLux?: number | null;
  noiseLevelDb?: number | null;
  batteryLevelPct?: number | null;
  measuredAtUtc: string;
  receivedAtUtc: string;
};

export type IngestReadingPayload = {
  temperatureC: number;
  humidityPct?: number;
  co2Ppm?: number;
  lightLevelLux?: number;
  noiseLevelDb?: number;
  batteryLevelPct?: number;
};

export type User = {
  id: string;
  userName: string;
  email: string;
  firstName: string;
  lastName: string;
  isActive: boolean;
  roles: string[];
};

export type CompanyUser = {
  id: string;
  companyId: string;
  userId: string;
  role: string;
  assignedAtUtc: string;
};

export type CompanySummary = {
  company: Company;
  deviceCount: number;
  activeAlerts: number;
  devicesOk: number;
  devicesAlerting: number;
  devicesOffline: number;
};

export type RegisterResponse = {
  id: string;
  userName: string;
  email: string;
  createdAt: string;
  emailConfirmationRequired: boolean;
};

export type AuditLog = {
  id: string;
  serviceName: string;
  eventType: string;
  outcome: string;
  actorUserId?: string | null;
  actorUserName?: string | null;
  targetEntityType?: string | null;
  targetEntityId?: string | null;
  targetUserName?: string | null;
  detail?: string | null;
  correlationId?: string | null;
  ipAddress?: string | null;
  occurredAtUtc: string;
};

export type PagedAuditLogs = {
  items: AuditLog[];
  page: number;
  pageSize: number;
  totalCount: number;
};
