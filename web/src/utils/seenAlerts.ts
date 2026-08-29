const STORAGE_PREFIX = 'sm_seen_alerts_';

export function loadSeenAlertIds(userId: string | null): Set<string> {
  if (!userId) {
    return new Set();
  }

  try {
    const raw = localStorage.getItem(`${STORAGE_PREFIX}${userId}`);
    if (!raw) {
      return new Set();
    }

    const parsed = JSON.parse(raw) as unknown;
    if (!Array.isArray(parsed)) {
      return new Set();
    }

    return new Set(parsed.filter((id): id is string => typeof id === 'string'));
  } catch {
    return new Set();
  }
}

export function saveSeenAlertIds(userId: string | null, ids: Set<string>) {
  if (!userId) {
    return;
  }

  localStorage.setItem(`${STORAGE_PREFIX}${userId}`, JSON.stringify([...ids]));
}
