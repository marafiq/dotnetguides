import { defineConfig, devices } from '@playwright/test';

/**
 * Playwright config for client-only E2E tests
 * Uses Vite dev server with mock data (no .NET backend required)
 */
export default defineConfig({
  testDir: './tests-client',
  fullyParallel: false,
  forbidOnly: !!process.env.CI,
  retries: 0,
  workers: 1,
  reporter: [
    ['html', { open: 'never' }],
    ['list'],
  ],
  outputDir: './test-results-client',

  use: {
    baseURL: 'http://localhost:5173',
    trace: 'on',
    screenshot: 'on',
    video: 'retain-on-failure',
  },

  expect: {
    timeout: 10000,
  },

  projects: [
    {
      name: 'chromium',
      use: {
        ...devices['Desktop Chrome'],
        viewport: { width: 1440, height: 900 },
      },
    },
  ],

  webServer: {
    command: 'npm run dev --prefix ../../samples/SampleApp/client -- --host 0.0.0.0 --port 5173',
    url: 'http://localhost:5173',
    reuseExistingServer: !process.env.CI,
    timeout: 30000,
    stdout: 'pipe',
    stderr: 'pipe',
  },
});
