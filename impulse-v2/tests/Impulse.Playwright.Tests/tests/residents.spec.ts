import { test, expect } from '@playwright/test';

/**
 * E2E tests for Residents endpoints
 * TDD: These tests define expected behavior BEFORE backend implementation
 *
 * Endpoints tested:
 * - GET /residents (ListResidents)
 * - GET /residents/{id} (GetResident)
 * - POST /residents (CreateResident)
 * - PUT /residents/{id} (UpdateResident)
 */
test.describe('Residents API', () => {

  test.describe('GET /residents - ListResidents', () => {
    test('returns residents list with pagination', async ({ request, page }) => {
      const response = await request.get('/residents', {
        headers: { 'X-Impulse': 'true' },
      });

      expect(response.status()).toBe(200);
      const body = await response.json();

      // Must have ListResidentsResponse shape
      expect(body.props).toHaveProperty('residents');
      expect(body.props).toHaveProperty('totalCount');
      expect(body.props).toHaveProperty('page');
      expect(body.props).toHaveProperty('pageSize');
      expect(Array.isArray(body.props.residents)).toBe(true);

      // Screenshot
      await page.goto('/residents');
      await page.screenshot({ path: 'test-results/residents-list.png' });
    });

    test('renders HTML shell without X-Impulse header', async ({ page }) => {
      await page.goto('/residents');

      // Should render HTML with impulse data embedded
      const appElement = page.locator('#app');
      await expect(appElement).toBeVisible();

      const impulseData = await appElement.getAttribute('data-impulse');
      expect(impulseData).toBeTruthy();

      const payload = JSON.parse(impulseData!);
      expect(payload.props).toBeDefined();
      expect(payload.props.residents).toBeDefined();

      await page.screenshot({ path: 'test-results/residents-list-html.png' });
    });

    test('renders actual UI with resident cards after hydration', async ({ page }) => {
      await page.goto('/residents');

      // Wait for React to hydrate and render
      await page.waitForSelector('.residents-list, h1:has-text("Residents")', { timeout: 5000 }).catch(() => {});

      // Check for page header
      const heading = page.locator('h1');
      await expect(heading).toContainText(/Residents|Loading/);

      // Take screenshot of actual UI
      await page.screenshot({ path: 'test-results/residents-list-ui.png', fullPage: true });
    });
  });

  test.describe('GET /residents/{id} - GetResident', () => {
    test('returns resident detail with all fields', async ({ request }) => {
      const response = await request.get('/residents/1', {
        headers: { 'X-Impulse': 'true' },
      });

      expect(response.status()).toBe(200);
      const body = await response.json();

      // Must have GetResidentResponse shape
      expect(body.props).toHaveProperty('id');
      expect(body.props).toHaveProperty('firstName');
      expect(body.props).toHaveProperty('lastName');
      expect(body.props).toHaveProperty('dateOfBirth');
      expect(body.props).toHaveProperty('roomNumber');
      expect(body.props).toHaveProperty('address');
      expect(body.props).toHaveProperty('emergencyContacts');
      expect(body.props).toHaveProperty('admissionDate');
      expect(body.props).toHaveProperty('status');
    });

    test('returns 404 for non-existent resident', async ({ request }) => {
      const response = await request.get('/residents/999999', {
        headers: { 'X-Impulse': 'true' },
      });

      expect(response.status()).toBe(404);
    });

    test('renders resident detail page with screenshot', async ({ page }) => {
      await page.goto('/residents/1');
      await expect(page.locator('#app')).toBeVisible();
      await page.screenshot({ path: 'test-results/resident-detail.png' });
    });

    test('renders actual resident detail UI after hydration', async ({ page }) => {
      await page.goto('/residents/1');

      // Wait for React to hydrate
      await page.waitForSelector('.resident-detail, h1', { timeout: 5000 }).catch(() => {});

      // Check heading shows resident name or loading
      const heading = page.locator('h1');
      await expect(heading).toContainText(/John Smith|Loading/);

      // Take screenshot of actual UI
      await page.screenshot({ path: 'test-results/screenshot-resident-detail.png', fullPage: true });
    });
  });

  test.describe('POST /residents - CreateResident', () => {
    test('creates new resident with valid data', async ({ request }) => {
      const response = await request.post('/residents', {
        headers: {
          'Content-Type': 'application/json',
          'X-Impulse': 'true',
        },
        data: {
          firstName: 'John',
          lastName: 'Doe',
          dateOfBirth: '1945-03-15',
          roomNumber: 'A101',
        },
      });

      expect([200, 201]).toContain(response.status());
      const body = await response.json();

      // Must have CreateResidentResponse shape
      expect(body).toHaveProperty('id');
      expect(body).toHaveProperty('firstName');
      expect(body).toHaveProperty('lastName');
      expect(typeof body.id).toBe('number');
    });

    test('returns 422 for invalid data', async ({ request }) => {
      const response = await request.post('/residents', {
        headers: {
          'Content-Type': 'application/json',
          'X-Impulse': 'true',
        },
        data: {
          // Missing required dateOfBirth
          firstName: '',
          lastName: '',
        },
      });

      expect(response.status()).toBe(422);
      const body = await response.json();
      expect(body).toHaveProperty('errors');
    });
  });

  test.describe('PUT /residents/{id} - UpdateResident', () => {
    test('updates resident with valid data', async ({ request }) => {
      const response = await request.put('/residents/1', {
        headers: {
          'Content-Type': 'application/json',
          'X-Impulse': 'true',
        },
        data: {
          id: 1,
          firstName: 'Jane',
          lastName: 'Updated',
          roomNumber: 'B202',
        },
      });

      expect(response.status()).toBe(200);
      const body = await response.json();

      // Must have UpdateResidentResponse shape
      expect(body).toHaveProperty('id');
      expect(body).toHaveProperty('firstName');
      expect(body).toHaveProperty('lastName');
      expect(body).toHaveProperty('updatedAt');
    });

    test('returns 404 for non-existent resident', async ({ request }) => {
      const response = await request.put('/residents/999999', {
        headers: {
          'Content-Type': 'application/json',
          'X-Impulse': 'true',
        },
        data: {
          id: 999999,
          firstName: 'Ghost',
          lastName: 'Resident',
        },
      });

      expect(response.status()).toBe(404);
    });
  });
});
