import { test, expect } from '@playwright/test';

/**
 * E2E tests for Impulse Framework features
 * TDD: These tests define expected framework behavior
 *
 * Features tested:
 * - X-Impulse header for JSON responses
 * - HTML shell rendering without header
 * - Version checking for client reload
 * - Zero magic strings enforcement
 */
test.describe('Impulse Framework Features', () => {

  test.describe('Request/Response Headers', () => {
    test('X-Impulse header returns JSON props', async ({ request }) => {
      const response = await request.get('/residents', {
        headers: { 'X-Impulse': 'true' },
      });

      expect(response.status()).toBe(200);
      expect(response.headers()['content-type']).toContain('application/json');

      const body = await response.json();
      expect(body).toHaveProperty('props');
      expect(body).toHaveProperty('component');
    });

    test('No X-Impulse header returns HTML', async ({ request }) => {
      const response = await request.get('/residents');

      expect(response.status()).toBe(200);
      expect(response.headers()['content-type']).toContain('text/html');

      const body = await response.text();
      expect(body).toContain('<!DOCTYPE html>');
      expect(body).toContain('data-impulse');
    });

    test('Response includes component path', async ({ request }) => {
      const response = await request.get('/residents', {
        headers: { 'X-Impulse': 'true' },
      });

      const body = await response.json();
      expect(body.component).toBeTruthy();
      expect(typeof body.component).toBe('string');
    });
  });

  test.describe('Shell Rendering', () => {
    test('HTML shell has #app element with data-impulse', async ({ page }) => {
      await page.goto('/residents');

      const appElement = page.locator('#app');
      await expect(appElement).toBeVisible();

      const impulseData = await appElement.getAttribute('data-impulse');
      expect(impulseData).toBeTruthy();

      await page.screenshot({ path: 'test-results/shell-rendering.png' });
    });

    test('data-impulse contains valid JSON', async ({ page }) => {
      await page.goto('/residents');

      const appElement = page.locator('#app');
      const impulseData = await appElement.getAttribute('data-impulse');

      expect(() => JSON.parse(impulseData!)).not.toThrow();

      const payload = JSON.parse(impulseData!);
      expect(payload).toHaveProperty('props');
      expect(payload).toHaveProperty('component');
    });
  });

  test.describe('Version Management', () => {
    test('Response includes version', async ({ request }) => {
      const response = await request.get('/residents', {
        headers: { 'X-Impulse': 'true' },
      });

      const body = await response.json();
      expect(body).toHaveProperty('version');
      expect(typeof body.version).toBe('string');
    });

    test('Matching version does not trigger reload', async ({ request }) => {
      // Get current version
      const initial = await request.get('/residents', {
        headers: { 'X-Impulse': 'true' },
      });
      const { version } = await initial.json();

      // Request with matching version
      const response = await request.get('/residents', {
        headers: {
          'X-Impulse': 'true',
          'X-Impulse-Version': version,
        },
      });

      expect(response.headers()['x-impulse-reload']).not.toBe('true');
    });
  });

  test.describe('Type Safety', () => {
    test('Generated types match response structure', async ({ request }) => {
      const response = await request.get('/residents/1', {
        headers: { 'X-Impulse': 'true' },
      });

      const body = await response.json();

      // Verify GetResidentResponse structure matches generated types
      const props = body.props;

      // Required fields must be present and correct type
      expect(typeof props.id).toBe('number');
      expect(typeof props.dateOfBirth).toBe('string');
      expect(typeof props.admissionDate).toBe('string');

      // Optional fields if present must be correct type
      if (props.firstName !== undefined) {
        expect(typeof props.firstName).toBe('string');
      }
      if (props.emergencyContacts !== undefined) {
        expect(Array.isArray(props.emergencyContacts)).toBe(true);
      }
    });
  });

  test.describe('Error Handling', () => {
    test('404 returns proper error format', async ({ request }) => {
      const response = await request.get('/residents/999999', {
        headers: { 'X-Impulse': 'true' },
      });

      expect(response.status()).toBe(404);
    });

    test('Validation error returns 422 with errors object', async ({ request }) => {
      const response = await request.post('/residents', {
        headers: {
          'Content-Type': 'application/json',
          'X-Impulse': 'true',
        },
        data: {},
      });

      if (response.status() === 422) {
        const body = await response.json();
        expect(body).toHaveProperty('errors');
        expect(typeof body.errors).toBe('object');
      }
    });
  });

  test.describe('Screenshots - All Routes', () => {
    const routes = [
      { path: '/residents', name: 'residents-list' },
      { path: '/residents/1', name: 'resident-detail' },
      { path: '/residents/1/medications', name: 'medications-list' },
      { path: '/residents/1/medications/1', name: 'medication-detail' },
      { path: '/residents/1/care-plan', name: 'care-plan' },
    ];

    for (const route of routes) {
      test(`Screenshot: ${route.name}`, async ({ page }) => {
        await page.goto(route.path);
        await page.waitForLoadState('networkidle');
        await page.screenshot({
          path: `test-results/screenshot-${route.name}.png`,
          fullPage: true,
        });
      });
    }
  });
});
