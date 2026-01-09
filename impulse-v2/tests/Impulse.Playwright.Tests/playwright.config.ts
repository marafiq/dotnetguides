import { defineConfig, devices } from '@playwright/test';

/**
 * Playwright E2E tests for Impulse v2
 * TDD: These tests define expected behavior BEFORE implementation
 */
export default defineConfig({
  testDir: './tests',
  fullyParallel: false,
  forbidOnly: !!process.env.CI,
  retries: process.env.CI ? 2 : 0,
  workers: 1,
  reporter: [
    ['html', { open: 'never' }],
    ['list'],
  ],
  outputDir: './test-results',

  use: {
    baseURL: 'http://localhost:5000',
    trace: 'on',
    screenshot: 'on',
    video: 'on-first-retry',
    viewport: { width: 1280, height: 800 },
  },

  expect: {
    timeout: 10000,
    toHaveScreenshot: {
      maxDiffPixels: 100,
    },
  },

  projects: [
    {
      name: 'chromium',
      use: { ...devices['Desktop Chrome'] },
    },
  ],

  webServer: {
    command: `${process.env.HOME}/.dotnet/dotnet run --project ../../samples/SampleApp/SampleApp.csproj --urls http://localhost:5000`,
    url: 'http://localhost:5000',
    reuseExistingServer: !process.env.CI,
    timeout: 60000,
    stdout: 'pipe',
    stderr: 'pipe',
    env: {
      HOME: process.env.HOME,
      DOTNET_ROOT: `${process.env.HOME}/.dotnet`,
      PATH: `${process.env.HOME}/.dotnet:${process.env.PATH}`,
      ASPNETCORE_ENVIRONMENT: 'Development',
    },
  },
});
