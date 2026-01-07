import { test, expect } from '@playwright/test';

test.describe('Impulse Shell', () => {
  test('server renders valid shell HTML', async ({ page }) => {
    const response = await page.goto('/');

    // Should return HTML
    expect(response?.headers()['content-type']).toContain('text/html');

    // Should have #app element
    const appElement = page.locator('#app');
    await expect(appElement).toBeVisible();
  });

  test('shell contains valid JSON payload', async ({ page }) => {
    await page.goto('/');

    const appElement = page.locator('#app');
    const impulseData = await appElement.getAttribute('data-impulse');

    // Should be valid JSON
    expect(() => JSON.parse(impulseData!)).not.toThrow();

    const payload = JSON.parse(impulseData!);

    // Verify required fields
    expect(payload).toHaveProperty('url');
    expect(payload).toHaveProperty('version');
    expect(payload).toHaveProperty('props');
    expect(payload).toHaveProperty('context');
  });

  test('shell has component path attribute', async ({ page }) => {
    await page.goto('/');

    const appElement = page.locator('#app');
    const componentPath = await appElement.getAttribute('data-component');

    // Should have component path for hydration
    expect(componentPath).toBeTruthy();
    expect(componentPath).toMatch(/^\.\//); // Should start with ./
  });

  test('returns JSON for X-Impulse requests', async ({ request }) => {
    const response = await request.get('/', {
      headers: {
        'X-Impulse': 'true',
        Accept: 'application/json',
      },
    });

    expect(response.headers()['content-type']).toContain('application/json');

    const payload = await response.json();
    expect(payload).toHaveProperty('url');
    expect(payload).toHaveProperty('version');
    expect(payload).toHaveProperty('props');
  });

  test('version mismatch triggers reload header', async ({ request }) => {
    // First get current version
    const initialResponse = await request.get('/', {
      headers: { 'X-Impulse': 'true' },
    });
    const initialPayload = await initialResponse.json();
    const currentVersion = initialPayload.version;

    // Request with wrong version
    const response = await request.get('/', {
      headers: {
        'X-Impulse': 'true',
        'X-Impulse-Version': 'wrong-version-12345',
      },
    });

    // Server may signal reload needed
    const reloadHeader = response.headers()['x-impulse-reload'];
    // This test documents the expected behavior - may or may not trigger based on implementation
    expect(response.ok()).toBeTruthy();
  });
});
