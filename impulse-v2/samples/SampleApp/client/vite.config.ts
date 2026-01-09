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
    },
    // Ensure dependencies resolve from client's node_modules
    dedupe: ['react', 'react-dom', '@tanstack/react-router', '@tanstack/react-query'],
  },
  // Include Features and generated directories in optimization
  optimizeDeps: {
    include: ['react', 'react-dom', '@tanstack/react-router'],
  },
  build: {
    outDir: '../wwwroot/assets',
    emptyOutDir: true,
    rollupOptions: {
      input: './main.tsx',
      output: {
        entryFileNames: 'main.js',
        chunkFileNames: '[name]-[hash].js',
        assetFileNames: '[name]-[hash][extname]',
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
