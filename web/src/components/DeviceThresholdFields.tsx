import type { DeviceThresholdForm } from '../constants/deviceDefaults';

type Props = {
  form: DeviceThresholdForm;
  onChange: (next: DeviceThresholdForm) => void;
  showBattery?: boolean;
};

export function DeviceThresholdFields({ form, onChange, showBattery = true }: Props) {
  function updateField<K extends keyof DeviceThresholdForm>(key: K, value: string) {
    onChange({ ...form, [key]: value });
  }

  return (
    <>
      <div className="inline-fields">
        <label>
          Min °C
          <input type="number" step="0.1" value={form.minTempC} onChange={(e) => updateField('minTempC', e.target.value)} required />
        </label>
        <label>
          Max °C
          <input type="number" step="0.1" value={form.maxTempC} onChange={(e) => updateField('maxTempC', e.target.value)} required />
        </label>
      </div>
      <div className="inline-fields">
        <label>
          Min humidity %
          <input type="number" step="0.1" value={form.minHumidityPct} onChange={(e) => updateField('minHumidityPct', e.target.value)} required />
        </label>
        <label>
          Max humidity %
          <input type="number" step="0.1" value={form.maxHumidityPct} onChange={(e) => updateField('maxHumidityPct', e.target.value)} required />
        </label>
      </div>
      <div className="inline-fields">
        <label>
          Min CO₂ ppm
          <input type="number" step="1" value={form.minCo2Ppm} onChange={(e) => updateField('minCo2Ppm', e.target.value)} required />
        </label>
        <label>
          Max CO₂ ppm
          <input type="number" step="1" value={form.maxCo2Ppm} onChange={(e) => updateField('maxCo2Ppm', e.target.value)} required />
        </label>
      </div>
      <div className="inline-fields">
        <label>
          Min light lux
          <input type="number" step="1" value={form.minLightLevelLux} onChange={(e) => updateField('minLightLevelLux', e.target.value)} required />
        </label>
        <label>
          Max light lux
          <input type="number" step="1" value={form.maxLightLevelLux} onChange={(e) => updateField('maxLightLevelLux', e.target.value)} required />
        </label>
      </div>
      <div className="inline-fields">
        <label>
          Min noise dB
          <input type="number" step="0.1" value={form.minNoiseLevelDb} onChange={(e) => updateField('minNoiseLevelDb', e.target.value)} required />
        </label>
        <label>
          Max noise dB
          <input type="number" step="0.1" value={form.maxNoiseLevelDb} onChange={(e) => updateField('maxNoiseLevelDb', e.target.value)} required />
        </label>
      </div>
      {showBattery && (
        <label>
          Min battery %
          <input type="number" step="1" value={form.minBatteryPct} onChange={(e) => updateField('minBatteryPct', e.target.value)} required />
        </label>
      )}
    </>
  );
}
