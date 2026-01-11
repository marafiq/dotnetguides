import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';

export default defineConfig({
  plugins: [react()],
  server: {
    port: 5173,
    proxy: {
      '/residents': 'http://localhost:5000',
      '/admission': 'http://localhost:5000',
    },
  },
  build: {
    outDir: '../CareHome.Server/wwwroot',
    emptyOutDir: true,
  },
});
