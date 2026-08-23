export type JwtPayload = {
  sub?: string;
  userId?: string;
  userName?: string;
  roles: string[];
};

const ROLE_CLAIMS = [
  'role',
  'roles',
  'http://schemas.microsoft.com/ws/2008/06/identity/claims/role',
];

const NAME_CLAIMS = [
  'unique_name',
  'name',
  'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name',
];

export function parseJwt(token: string): JwtPayload | null {
  try {
    const payloadPart = token.split('.')[1];
    if (!payloadPart) return null;

    const json = JSON.parse(atob(payloadPart.replace(/-/g, '+').replace(/_/g, '/'))) as Record<
      string,
      unknown
    >;

    const roles = new Set<string>();
    for (const key of ROLE_CLAIMS) {
      const value = json[key];
      if (typeof value === 'string') roles.add(value);
      if (Array.isArray(value)) {
        value.filter((item): item is string => typeof item === 'string').forEach((item) => roles.add(item));
      }
    }

    const userName =
      NAME_CLAIMS.map((key) => json[key]).find((value): value is string => typeof value === 'string') ??
      undefined;

    const sub = typeof json.sub === 'string' ? json.sub : undefined;

    return {
      sub,
      userId: sub,
      userName,
      roles: [...roles],
    };
  } catch {
    return null;
  }
}

export function isAdmin(roles: string[]) {
  return roles.includes('Admin');
}
