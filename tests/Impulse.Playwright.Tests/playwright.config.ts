import { defineConfig, devices } from '@playwright/test';

/**
 * Playwright configuration for Impulse Framework E2E tests
 * Tests against the IntegrationTests server
 */
export default defineConfig({
  testDir: './tests',
  fullyParallel: true,
  forbidOnly: !!process.env.CI,
  retries: process.env.CI ? 2 : 0,
  workers: process.env.CI ? 1 : undefined,
  reporter: 'html',

  use: {
    baseURL: 'http://localhost:5000',
    trace: 'on-first-retry',
    screenshot: 'only-on-failure',
  },

  projects: [
    {
      name: 'chromium',
      use: { ...devices['Desktop Chrome'] },
    },
  ],

  // Start the IntegrationTests server before running tests
  webServer: {
    command: 'dotnet run --project ../Impulse.IntegrationTests/Impulse.IntegrationTests.csproj',
    url: 'http://localhost:5000',
    reuseExistingServer: !process.env.CI,
    timeout: 60000,
    env: {
      ASPNETCORE_URLS: 'http://localhost:5000',
      DOTNET_ENVIRONMENT: 'Development',
    },
  },
});
