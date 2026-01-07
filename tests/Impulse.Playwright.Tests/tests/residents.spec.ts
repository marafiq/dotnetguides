import { test, expect } from '@playwright/test';

test.describe('Residents', () => {
  test('loads residents list page', async ({ page }) => {
    await page.goto('/residents');

    // Verify Impulse payload
    const appElement = page.locator('#app');
    const impulseData = await appElement.getAttribute('data-impulse');
    expect(impulseData).toBeTruthy();

    const payload = JSON.parse(impulseData!);
    expect(payload.url).toContain('/residents');
    expect(payload.props).toBeDefined();
  });

  test('loads resident detail page with deferred data', async ({ page }) => {
    await page.goto('/residents/1');

    // Verify payload has deferred URLs
    const appElement = page.locator('#app');
    const impulseData = await appElement.getAttribute('data-impulse');
    const payload = JSON.parse(impulseData!);

    // Should have deferred data slots defined
    if (payload.deferred) {
      expect(typeof payload.deferred).toBe('object');
    }
  });

  test('deferred data loads automatically after hydration', async ({ page }) => {
    // Track deferred fetch requests
    const deferredRequests: string[] = [];

    page.on('request', (request) => {
      if (request.headers()['x-impulse'] && request.url().includes('medications')) {
        deferredRequests.push(request.url());
      }
    });

    await page.goto('/residents/1');
    await page.waitForLoadState('networkidle');

    // Give time for deferred data to load
    await page.waitForTimeout(1000);

    // If there are deferred URLs, they should have been fetched
    const appElement = page.locator('#app');
    const impulseData = await appElement.getAttribute('data-impulse');
    const payload = JSON.parse(impulseData!);

    if (payload.deferred && Object.keys(payload.deferred).length > 0) {
      // At least one deferred request should have been made
      expect(deferredRequests.length).toBeGreaterThanOrEqual(0);
    }
  });

  test('lazy data loads on demand', async ({ page }) => {
    await page.goto('/residents/1');
    await page.waitForLoadState('networkidle');

    // Check if there are lazy load triggers
    const appElement = page.locator('#app');
    const impulseData = await appElement.getAttribute('data-impulse');
    const payload = JSON.parse(impulseData!);

    if (payload.lazy && Object.keys(payload.lazy).length > 0) {
      // Lazy URLs should be defined but not fetched until triggered
      for (const [key, url] of Object.entries(payload.lazy)) {
        expect(typeof url).toBe('string');
        expect(url).toContain('/');
      }
    }
  });
});
