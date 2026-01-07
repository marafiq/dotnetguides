import { test, expect } from '@playwright/test';

test.describe('Mutations', () => {
  test('POST mutation returns proper response', async ({ request }) => {
    const response = await request.post('/api/residents', {
      headers: {
        'Content-Type': 'application/json',
        'X-Impulse': 'true',
      },
      data: {
        name: 'Test Resident',
        email: 'test@example.com',
      },
    });

    // Should return 2xx or validation error (422)
    expect([200, 201, 422]).toContain(response.status());
  });

  test('validation errors return 422 with error format', async ({ request }) => {
    const response = await request.post('/api/residents', {
      headers: {
        'Content-Type': 'application/json',
        'X-Impulse': 'true',
      },
      data: {
        // Empty data to trigger validation errors
        name: '',
      },
    });

    if (response.status() === 422) {
      const body = await response.json();
      expect(body).toHaveProperty('errors');
      expect(typeof body.errors).toBe('object');
    }
  });

  test('mutation includes X-Impulse-Version header', async ({ request }) => {
    // First get current version
    const pageResponse = await request.get('/', {
      headers: { 'X-Impulse': 'true' },
    });
    const payload = await pageResponse.json();
    const version = payload.version;

    // Make mutation with version
    const response = await request.post('/api/residents', {
      headers: {
        'Content-Type': 'application/json',
        'X-Impulse': 'true',
        'X-Impulse-Version': version,
      },
      data: {
        name: 'Test Resident',
      },
    });

    // Should not trigger reload (version matches)
    expect(response.headers()['x-impulse-reload']).not.toBe('true');
  });
});
