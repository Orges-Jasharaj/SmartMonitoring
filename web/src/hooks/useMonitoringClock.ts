import { useEffect, useState } from 'react';

const DEFAULT_INTERVAL_MS = 60_000;

export function useMonitoringClock(intervalMs = DEFAULT_INTERVAL_MS) {
  const [now, setNow] = useState(() => new Date());

  useEffect(() => {
    const timer = window.setInterval(() => {
      setNow(new Date());
    }, intervalMs);

    return () => {
      window.clearInterval(timer);
    };
  }, [intervalMs]);

  return now;
}
