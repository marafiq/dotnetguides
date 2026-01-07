import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';
import { resolve } from 'path';

// https://vitejs.dev/config/
export default defineConfig(({ command }) => ({
  plugins: [react()],

  // Entry point for the Impulse runtime
  build: {
    // Output to wwwroot for .NET static file serving
    outDir: 'wwwroot/js',
    emptyDirBeforeWrite: true,

    // Generate manifest.json for asset resolution
    manifest: true,

    rollupOptions: {
      input: {
        impulse: resolve(__dirname, 'Features/Shared/index.ts'),
      },
      output: {
        // Hashed filenames in production
        entryFileNames: '[name]-[hash].js',
        chunkFileNames: 'chunks/[name]-[hash].js',
        assetFileNames: 'assets/[name]-[hash][extname]',
      },
    },
  },

  // Dev server configuration for HMR
  server: {
    port: 5173,
    strictPort: true,
    hmr: {
      // Allow .NET backend to proxy HMR websocket
      protocol: 'ws',
      host: 'localhost',
    },
    // CORS for cross-origin requests from .NET backend
    cors: true,
    // Origin header for .NET proxy
    origin: 'http://localhost:5173',
  },

  // Resolve TypeScript paths
  resolve: {
    alias: {
      '@': resolve(__dirname, 'Features'),
    },
  },

  // Optimize dependencies
  optimizeDeps: {
    include: ['react', 'react-dom'],
  },
}));
