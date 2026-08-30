import { useState } from 'react';
import { api } from '../api/client';
import type { Alert } from '../api/types';

type AlertAcknowledgeButtonProps = {
  token: string | null;
  companyId: string;
  alert: Alert;
  onAcknowledged?: (alert: Alert) => void;
  onError?: (message: string) => void;
  className?: string;
};

export function AlertAcknowledgeButton({
  token,
  companyId,
  alert,
  onAcknowledged,
  onError,
  className = 'btn btn-ghost btn-sm',
}: AlertAcknowledgeButtonProps) {
  const [submitting, setSubmitting] = useState(false);

  if (!alert.isActive) {
    return null;
  }

  async function handleClick() {
    if (!token || submitting) return;

    setSubmitting(true);
    const response = await api.acknowledgeAlert(token, companyId, alert.id);
    setSubmitting(false);

    if (!response.success || !response.data) {
      onError?.(response.message ?? 'Failed to acknowledge alert');
      return;
    }

    onAcknowledged?.(response.data);
  }

  return (
    <button type="button" className={className} disabled={submitting} onClick={() => void handleClick()}>
      {submitting ? 'Acknowledging…' : 'Acknowledge'}
    </button>
  );
}
