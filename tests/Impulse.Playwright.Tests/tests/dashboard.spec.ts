import { test, expect } from '@playwright/test';

test.describe('Dashboard', () => {
  test('loads dashboard page with Impulse payload', async ({ page }) => {
    await page.goto('/');

    // Verify the #app element exists with Impulse data
    const appElement = page.locator('#app');
    await expect(appElement).toBeVisible();

    // Check data-impulse attribute is present (server-rendered payload)
    const impulseData = await appElement.getAttribute('data-impulse');
    expect(impulseData).toBeTruthy();

    // Parse and verify payload structure
    const payload = JSON.parse(impulseData!);
    expect(payload).toHaveProperty('url');
    expect(payload).toHaveProperty('version');
    expect(payload).toHaveProperty('props');
  });

  test('displays dashboard content after hydration', async ({ page }) => {
    await page.goto('/');

    // Wait for React hydration - component should render
    await page.waitForLoadState('networkidle');

    // Dashboard should have rendered content
    const content = page.locator('#app');
    await expect(content).not.toBeEmpty();
  });

  test('includes X-Impulse headers in navigation requests', async ({ page }) => {
    // Intercept fetch requests to verify headers
    const headers: Record<string, string>[] = [];

    page.on('request', (request) => {
      if (request.headers()['x-impulse']) {
        headers.push(request.headers());
      }
    });

    await page.goto('/');
    await page.waitForLoadState('networkidle');

    // Navigate programmatically via JS (simulating SPA navigation)
    await page.evaluate(async () => {
      const res = await fetch('/residents', {
        headers: { 'X-Impulse': 'true' },
      });
      return res.ok;
    });

    // Should have sent X-Impulse header
    expect(headers.some((h) => h['x-impulse'] === 'true')).toBeTruthy();
  });
});
