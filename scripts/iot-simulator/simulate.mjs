#!/usr/bin/env node
/**
 * Simulates IoT devices by posting environmental readings every 30–60 seconds.
 *
 * Setup:
 *   1. Copy devices.example.json to devices.json
 *   2. Add device keys from the UI (Device key → Show device key)
 *   3. node scripts/iot-simulator/simulate.mjs
 *
 * Env:
 *   IOT_GATEWAY_URL  default http://localhost:8088
 *   IOT_CONFIG       default scripts/iot-simulator/devices.json
 *   IOT_MIN_INTERVAL default 30000 (ms)
 *   IOT_MAX_INTERVAL default 60000 (ms)
 */

import { readFileSync } from 'node:fs';
import { dirname, join, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';

const __dirname = dirname(fileURLToPath(import.meta.url));

const gatewayUrl = (process.env.IOT_GATEWAY_URL ?? 'http://localhost:8088').replace(/\/$/, '');
const configPath = resolve(process.env.IOT_CONFIG ?? join(__dirname, 'devices.json'));
const minIntervalMs = Number(process.env.IOT_MIN_INTERVAL ?? 30_000);
const maxIntervalMs = Number(process.env.IOT_MAX_INTERVAL ?? 60_000);

/** @type {{ name: string; deviceKey: string }[]} */
let devices;

try {
  devices = JSON.parse(readFileSync(configPath, 'utf8'));
} catch {
  console.error(`Could not read ${configPath}. Copy devices.example.json to devices.json and add device keys.`);
  process.exit(1);
}

if (!Array.isArray(devices) || devices.length === 0) {
  console.error('devices.json must be a non-empty array of { name, deviceKey }.');
  process.exit(1);
}

const state = new Map(
  devices.map((device) => [
    device.deviceKey,
    {
      name: device.name ?? 'device',
      temperatureC: randomBetween(3, 7),
      humidityPct: randomBetween(45, 65),
      co2Ppm: randomBetween(420, 750),
      lightLevelLux: randomBetween(80, 220),
      noiseLevelDb: randomBetween(38, 55),
      batteryLevelPct: randomBetween(70, 95),
    },
  ]),
);

function randomBetween(min, max) {
  return min + Math.random() * (max - min);
}

function drift(current, min, max, step) {
  const next = current + randomBetween(-step, step);
  return clamp(next, min, max);
}

function clamp(value, min, max) {
  return Math.min(max, Math.max(min, value));
}

function round(value, decimals = 1) {
  const factor = 10 ** decimals;
  return Math.round(value * factor) / factor;
}

function buildPayload(metrics) {
  // Occasionally spike CO₂ or temperature for alert demos.
  const co2Ppm = Math.random() < 0.08 ? randomBetween(1100, 1400) : metrics.co2Ppm;
  const temperatureC = Math.random() < 0.06 ? randomBetween(9, 12) : metrics.temperatureC;

  return {
    temperatureC: round(temperatureC, 1),
    humidityPct: round(metrics.humidityPct, 1),
    co2Ppm: round(co2Ppm, 0),
    lightLevelLux: round(metrics.lightLevelLux, 0),
    noiseLevelDb: round(metrics.noiseLevelDb, 1),
    batteryLevelPct: round(metrics.batteryLevelPct, 0),
  };
}

async function sendReading(deviceKey, name) {
  const metrics = state.get(deviceKey);
  if (!metrics) return;

  metrics.temperatureC = drift(metrics.temperatureC, 1, 12, 0.8);
  metrics.humidityPct = drift(metrics.humidityPct, 35, 80, 2);
  metrics.co2Ppm = drift(metrics.co2Ppm, 380, 900, 40);
  metrics.lightLevelLux = drift(metrics.lightLevelLux, 0, 450, 25);
  metrics.noiseLevelDb = drift(metrics.noiseLevelDb, 32, 68, 2);
  metrics.batteryLevelPct = drift(metrics.batteryLevelPct, 15, 100, 0.3);

  const payload = buildPayload(metrics);
  const url = `${gatewayUrl}/monitoring/api/ingest/readings`;

  const response = await fetch(url, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
      'X-Device-Key': deviceKey,
    },
    body: JSON.stringify(payload),
  });

  const body = await response.json().catch(() => ({}));
  const time = new Date().toISOString();

  if (!response.ok || body.success === false) {
    console.error(`[${time}] ${name} FAILED (${response.status}):`, body.message ?? 'Unknown error');
    return;
  }

  console.log(
    `[${time}] ${name} → temp ${payload.temperatureC}°C, humidity ${payload.humidityPct}%, CO₂ ${payload.co2Ppm} ppm`,
  );
}

function nextDelayMs() {
  return minIntervalMs + Math.random() * (maxIntervalMs - minIntervalMs);
}

async function tick() {
  await Promise.all(
    devices.map(({ name, deviceKey }) => {
      if (!deviceKey || deviceKey.includes('PASTE')) {
        console.warn(`Skipping ${name ?? 'device'} — set a real deviceKey in devices.json`);
        return Promise.resolve();
      }
      return sendReading(deviceKey, name ?? deviceKey.slice(0, 8));
    }),
  );
}

console.log(`IoT simulator started — ${devices.length} device(s), gateway ${gatewayUrl}`);
console.log(`Interval ${minIntervalMs / 1000}s – ${maxIntervalMs / 1000}s. Press Ctrl+C to stop.`);

await tick();

async function scheduleLoop() {
  setTimeout(async () => {
    await tick();
    scheduleLoop();
  }, nextDelayMs());
}

scheduleLoop();
