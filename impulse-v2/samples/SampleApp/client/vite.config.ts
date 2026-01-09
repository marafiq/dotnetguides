import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';
import path from 'path';

export default defineConfig({
  plugins: [react()],
  resolve: {
    alias: {
      '@impulse/react': path.resolve(__dirname, './impulse-runtime.ts'),
      '@generated': path.resolve(__dirname, '../generated'),
      '@features': path.resolve(__dirname, '../Features'),
      '@s2-styles': path.resolve(__dirname, './s2-styles.ts'),
      // Ensure zod resolves from client node_modules for Features folder
      'zod': path.resolve(__dirname, './node_modules/zod'),
    },
    dedupe: ['react', 'react-dom', '@tanstack/react-router', '@tanstack/react-query', '@react-spectrum/s2', 'zod'],
  },
  optimizeDeps: {
    include: ['react', 'react-dom', '@tanstack/react-router', '@react-spectrum/s2', 'zod'],
  },
  build: {
    target: ['es2022'],
    cssMinify: 'lightningcss',
    outDir: '../wwwroot/assets',
    emptyOutDir: true,
    rollupOptions: {
      input: './main.tsx',
      output: {
        entryFileNames: 'main.js',
        chunkFileNames: '[name]-[hash].js',
        assetFileNames: '[name]-[hash][extname]',
        // Bundle all S2 and style-macro CSS into a single bundle
        manualChunks(id) {
          if (/macro-(.*?)\.css$/.test(id) || /@react-spectrum\/s2\/.*\.css$/.test(id)) {
            return 's2-styles';
          }
        },
      },
    },
  },
  server: {
    proxy: {
      '/residents': 'http://localhost:5000',
      '/api': 'http://localhost:5000',
    },
  },
});
