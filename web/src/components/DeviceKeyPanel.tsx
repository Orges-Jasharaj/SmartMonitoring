import { useState } from 'react';
import { api } from '../api/client';
import { useAuth } from '../auth/AuthContext';
import { useToast } from './Toast';
import { copyToClipboard } from '../utils/monitoring';

type Props = {
  companyId: string;
  deviceId: string;
  deviceKey: string;
  onDeviceKeyChange: (key: string) => void;
};

export function DeviceKeyPanel({ companyId, deviceId, deviceKey, onDeviceKeyChange }: Props) {
  const { token } = useAuth();
  const { pushToast } = useToast();
  const [loading, setLoading] = useState(false);

  async function handleLoadKey() {
    if (!token) return;

    setLoading(true);
    const response = await api.getDeviceKey(token, companyId, deviceId);
    setLoading(false);

    if (!response.success || !response.data) {
      pushToast(response.message ?? 'Failed to load device key', 'error');
      return;
    }

    onDeviceKeyChange(response.data.deviceKey);
    pushToast('Device key loaded', 'success');
  }

  async function handleCopyKey() {
    if (!deviceKey.trim()) return;
    await copyToClipboard(deviceKey);
    pushToast('Device key copied to clipboard', 'success');
  }

  return (
    <div id="device-key" className="card stack highlight">
      <h2>Device key</h2>
      <p className="muted small">
        Company admins can reveal the <code>X-Device-Key</code> for this device to simulate IoT readings or test in Postman.
      </p>
      <div className="inline-actions">
        <button type="button" className="btn btn-secondary" disabled={loading} onClick={() => void handleLoadKey()}>
          {loading ? 'Loading…' : deviceKey ? 'Reload key' : 'Show device key'}
        </button>
        {deviceKey && (
          <button type="button" className="btn btn-ghost" onClick={() => void handleCopyKey()}>
            Copy key
          </button>
        )}
      </div>
      {deviceKey ? (
        <code className="device-key">{deviceKey}</code>
      ) : (
        <p className="muted small">The key is hidden until you click Show device key.</p>
      )}
    </div>
  );
}
