export type DeviceThresholdForm = {
  minTempC: string;
  maxTempC: string;
  minHumidityPct: string;
  maxHumidityPct: string;
  minCo2Ppm: string;
  maxCo2Ppm: string;
  minLightLevelLux: string;
  maxLightLevelLux: string;
  minNoiseLevelDb: string;
  maxNoiseLevelDb: string;
  minBatteryPct: string;
};

export const DEVICE_THRESHOLD_DEFAULTS: DeviceThresholdForm = {
  minTempC: '2',
  maxTempC: '8',
  minHumidityPct: '40',
  maxHumidityPct: '70',
  minCo2Ppm: '400',
  maxCo2Ppm: '1000',
  minLightLevelLux: '0',
  maxLightLevelLux: '500',
  minNoiseLevelDb: '35',
  maxNoiseLevelDb: '70',
  minBatteryPct: '20',
};

export function deviceThresholdFormFromDevice(device: {
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
}): DeviceThresholdForm {
  return {
    minTempC: String(device.minTempC),
    maxTempC: String(device.maxTempC),
    minHumidityPct: String(device.minHumidityPct),
    maxHumidityPct: String(device.maxHumidityPct),
    minCo2Ppm: String(device.minCo2Ppm),
    maxCo2Ppm: String(device.maxCo2Ppm),
    minLightLevelLux: String(device.minLightLevelLux),
    maxLightLevelLux: String(device.maxLightLevelLux),
    minNoiseLevelDb: String(device.minNoiseLevelDb),
    maxNoiseLevelDb: String(device.maxNoiseLevelDb),
    minBatteryPct: String(device.minBatteryPct),
  };
}

export function parseDeviceThresholdPayload(form: DeviceThresholdForm) {
  return {
    minTempC: Number(form.minTempC),
    maxTempC: Number(form.maxTempC),
    minHumidityPct: Number(form.minHumidityPct),
    maxHumidityPct: Number(form.maxHumidityPct),
    minCo2Ppm: Number(form.minCo2Ppm),
    maxCo2Ppm: Number(form.maxCo2Ppm),
    minLightLevelLux: Number(form.minLightLevelLux),
    maxLightLevelLux: Number(form.maxLightLevelLux),
    minNoiseLevelDb: Number(form.minNoiseLevelDb),
    maxNoiseLevelDb: Number(form.maxNoiseLevelDb),
    minBatteryPct: Number(form.minBatteryPct),
  };
}
