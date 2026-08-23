import type {
  Alert,
  ApiResponse,
  Company,
  CompanyUser,
  Device,
  DeviceCreated,
  JwtResult,
  PagedAuditLogs,
  Reading,
  RegisterResponse,
  User,
} from './types';

const API_BASE = import.meta.env.VITE_API_BASE_URL ?? '';

type RequestOptions = {
  method?: string;
  token?: string | null;
  body?: unknown;
  headers?: Record<string, string>;
};

async function request<T>(path: string, options: RequestOptions = {}): Promise<ApiResponse<T>> {
  const headers: Record<string, string> = {
    Accept: 'application/json',
    ...options.headers,
  };

  if (options.body !== undefined) {
    headers['Content-Type'] = 'application/json';
  }

  if (options.token) {
    headers.Authorization = `Bearer ${options.token}`;
  }

  const response = await fetch(`${API_BASE}${path}`, {
    method: options.method ?? (options.body !== undefined ? 'POST' : 'GET'),
    headers,
    body: options.body !== undefined ? JSON.stringify(options.body) : undefined,
  });

  const payload = (await response.json()) as ApiResponse<T>;
  if (!response.ok && payload.success !== false) {
    return {
      success: false,
      message: `Request failed (${response.status})`,
    };
  }

  return payload;
}

export const api = {
  login(userNameOrEmail: string, password: string) {
    return request<JwtResult>('/identity/api/authentication/login', {
      method: 'POST',
      body: { userNameOrEmail, password },
    });
  },

  register(payload: {
    userName: string;
    email: string;
    password: string;
    firstName: string;
    lastName: string;
    dateOfBirth: string;
  }) {
    return request<RegisterResponse>('/identity/api/authentication/register', {
      method: 'POST',
      body: payload,
    });
  },

  forgotPassword(emailOrUserName: string) {
    return request<boolean>('/identity/api/authentication/forgot-password', {
      method: 'POST',
      body: { emailOrUserName },
    });
  },

  resetPassword(userId: string, token: string, newPassword: string) {
    return request<boolean>('/identity/api/authentication/reset-password', {
      method: 'POST',
      body: { userId, token, newPassword },
    });
  },

  changePassword(token: string, oldPassword: string, newPassword: string) {
    return request<boolean>('/identity/api/users/me/change-password', {
      method: 'POST',
      token,
      body: { oldPassword, newPassword },
    });
  },

  getCompanies(token: string) {
    return request<Company[]>('/monitoring/api/companies', { token });
  },

  getCompany(token: string, companyId: string) {
    return request<Company>(`/monitoring/api/companies/${companyId}`, { token });
  },

  createCompany(token: string, name: string) {
    return request<Company>('/monitoring/api/companies', {
      method: 'POST',
      token,
      body: { name },
    });
  },

  getDevices(token: string, companyId: string) {
    return request<Device[]>(`/monitoring/api/companies/${companyId}/devices`, { token });
  },

  createDevice(
    token: string,
    companyId: string,
    payload: { name: string; zoneName: string; minTempC: number; maxTempC: number },
  ) {
    return request<DeviceCreated>(`/monitoring/api/companies/${companyId}/devices`, {
      method: 'POST',
      token,
      body: payload,
    });
  },

  getAlerts(token: string, companyId: string, activeOnly = true) {
    const query = activeOnly ? '?activeOnly=true' : '?activeOnly=false';
    return request<Alert[]>(`/monitoring/api/companies/${companyId}/alerts${query}`, { token });
  },

  getReadings(token: string, companyId: string, deviceId?: string, limit = 50) {
    const params = new URLSearchParams({ limit: String(limit) });
    if (deviceId) {
      params.set('deviceId', deviceId);
    }
    return request<Reading[]>(`/monitoring/api/companies/${companyId}/readings?${params}`, { token });
  },

  ingestReading(deviceKey: string, temperatureC: number) {
    return request<Reading>('/monitoring/api/ingest/readings', {
      method: 'POST',
      headers: { 'X-Device-Key': deviceKey },
      body: { temperatureC },
    });
  },

  getDevice(token: string, deviceId: string) {
    return request<Device>(`/monitoring/api/devices/${deviceId}`, { token });
  },

  getCompanyUsers(token: string, companyId: string) {
    return request<CompanyUser[]>(`/monitoring/api/companies/${companyId}/users`, { token });
  },

  getUsers(token: string) {
    return request<User[]>('/identity/api/users', { token });
  },

  activateUser(token: string, userId: string) {
    return request<boolean>(`/identity/api/users/activate/${userId}`, { method: 'POST', token });
  },

  deactivateUser(token: string, userId: string) {
    return request<boolean>(`/identity/api/users/deactivate/${userId}`, { method: 'POST', token });
  },

  getRoles(token: string) {
    return request<string[]>('/identity/api/roles', { token });
  },

  createRole(token: string, roleName: string) {
    return request<boolean>('/identity/api/roles', {
      method: 'POST',
      token,
      body: { roleName },
    });
  },

  assignRole(token: string, userId: string, roleName: string) {
    return request<boolean>('/identity/api/roles/assign', {
      method: 'POST',
      token,
      body: { userId, roleName },
    });
  },

  deleteRole(token: string, roleName: string) {
    return request<boolean>(`/identity/api/roles/${encodeURIComponent(roleName)}`, {
      method: 'DELETE',
      token,
    });
  },

  assignCompanyUser(token: string, companyId: string, userId: string, role: string) {
    return request<unknown>(`/monitoring/api/companies/${companyId}/users`, {
      method: 'POST',
      token,
      body: { userId, role },
    });
  },

  getAuditLogs(
    token: string,
    filters: {
      serviceName?: string;
      eventType?: string;
      actorUserId?: string;
      fromUtc?: string;
      toUtc?: string;
      page?: number;
      pageSize?: number;
    },
  ) {
    const params = new URLSearchParams();
    if (filters.serviceName) params.set('serviceName', filters.serviceName);
    if (filters.eventType) params.set('eventType', filters.eventType);
    if (filters.actorUserId) params.set('actorUserId', filters.actorUserId);
    if (filters.fromUtc) params.set('fromUtc', filters.fromUtc);
    if (filters.toUtc) params.set('toUtc', filters.toUtc);
    params.set('page', String(filters.page ?? 1));
    params.set('pageSize', String(filters.pageSize ?? 25));
    return request<PagedAuditLogs>(`/audit/api/audit?${params}`, { token });
  },
};
