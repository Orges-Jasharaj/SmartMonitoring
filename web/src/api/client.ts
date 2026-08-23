import type {
  Alert,
  ApiResponse,
  Company,
  Device,
  DeviceCreated,
  JwtResult,
  Reading,
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

  getUsers(token: string) {
    return request<User[]>('/identity/api/users', { token });
  },

  assignCompanyUser(token: string, companyId: string, userId: string, role: string) {
    return request<unknown>(`/monitoring/api/companies/${companyId}/users`, {
      method: 'POST',
      token,
      body: { userId, role },
    });
  },
};
