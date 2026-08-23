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
  isActive: boolean;
  lastReadingAtUtc?: string | null;
  createdAtUtc: string;
};

export type DeviceCreated = Device & {
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
  measuredAtUtc: string;
  receivedAtUtc: string;
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
};
