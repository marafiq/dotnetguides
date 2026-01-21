import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';
import { resolve } from 'path';

export default defineConfig({
  plugins: [react()],
  resolve: {
    alias: {
      '@features': resolve(__dirname, 'Features')
    }
  },
  build: {
    outDir: 'wwwroot/dist',
    manifest: true
  },
  server: {
    port: 3000
  }
});
