import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';
import { resolve } from 'path';
import { readdirSync, statSync, existsSync } from 'fs';

// Auto-discover TSX entry points from Features folders
function getFeatureEntries(featuresDir: string): Record<string, string> {
  const entries: Record<string, string> = {};

  if (!existsSync(featuresDir)) {
    return entries;
  }

  const features = readdirSync(featuresDir);
  for (const feature of features) {
    const featurePath = resolve(featuresDir, feature);
    if (statSync(featurePath).isDirectory()) {
      const files = readdirSync(featurePath);
      for (const file of files) {
        if (file.endsWith('.tsx')) {
          const name = file.replace('.tsx', '');
          entries[`${feature}/${name}`] = resolve(featurePath, file);
        }
      }
    }
  }

  return entries;
}

export default defineConfig({
  plugins: [react()],
  root: '.',
  publicDir: 'public',
  build: {
    outDir: 'wwwroot/dist',
    emptyOutDir: true,
    manifest: true,
    rollupOptions: {
      input: {
        main: resolve(__dirname, 'index.html'),
        ...getFeatureEntries(resolve(__dirname, 'Features'))
      },
      output: {
        // Organize chunks by feature
        chunkFileNames: 'assets/[name]-[hash].js',
        entryFileNames: 'assets/[name]-[hash].js',
        assetFileNames: 'assets/[name]-[hash].[ext]'
      }
    }
  },
  server: {
    port: 3000,
    proxy: {
      '/api': {
        target: 'http://localhost:5000',
        changeOrigin: true
      }
    }
  }
});
