import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';

export default defineConfig({
  plugins: [react()],
  server: {
    port: 5173,
    proxy: {
      '/identity': { target: 'http://localhost:8088', changeOrigin: true },
      '/monitoring': { target: 'http://localhost:8088', changeOrigin: true, ws: true },
      '/audit': { target: 'http://localhost:8088', changeOrigin: true },
    },
  },
});
