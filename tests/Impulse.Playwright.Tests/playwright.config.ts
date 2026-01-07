import { defineConfig, devices } from '@playwright/test';
import path from 'path';

/**
 * Playwright configuration for Impulse Framework E2E tests
 * Tests against the IntegrationTests server with comprehensive screenshot capture
 */
export default defineConfig({
  testDir: './tests',
  fullyParallel: false, // Run sequentially for deterministic screenshots
  forbidOnly: !!process.env.CI,
  retries: process.env.CI ? 2 : 0,
  workers: 1, // Single worker for predictable ordering
  reporter: [
    ['html', { open: 'never' }],
    ['list'],
  ],

  // Output directories for screenshots and reports
  outputDir: './test-results',

  use: {
    baseURL: 'http://localhost:5000',
    trace: 'on',
    screenshot: 'on', // Capture screenshots for all tests
    video: 'on-first-retry',
    viewport: { width: 1280, height: 800 },
  },

  // Expect settings
  expect: {
    timeout: 10000,
    toHaveScreenshot: {
      maxDiffPixels: 100, // Allow minor differences
    },
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
    timeout: 120000,
    env: {
      ASPNETCORE_URLS: 'http://localhost:5000',
      DOTNET_ENVIRONMENT: 'Production',  // Use production to load manifest
      PATH: `${process.env.HOME}/.dotnet:${process.env.PATH}`,
    },
  },
});
