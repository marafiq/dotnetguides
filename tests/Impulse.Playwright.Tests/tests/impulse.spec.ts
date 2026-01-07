import { test, expect } from '@playwright/test';

/**
 * Impulse Framework E2E Tests
 *
 * These tests verify the core Impulse features:
 * 1. Server-side shell rendering with hydration data
 * 2. Impulse JSON protocol (X-Impulse header)
 * 3. Route generation from source generators
 * 4. Complex nested props serialization
 */

test.describe('Impulse Shell Rendering', () => {
  test('renders HTML shell with #app root', async ({ page }) => {
    await page.goto('/');
    const app = page.locator('#app');
    await expect(app).toBeAttached();
    await page.screenshot({ path: 'screenshots/impulse-shell-root.png', fullPage: true });
  });

  test('shell contains data-impulse attribute with JSON payload', async ({ page }) => {
    await page.goto('/');
    const app = page.locator('#app');
    const impulseData = await app.getAttribute('data-impulse');

    expect(impulseData).toBeTruthy();
    const payload = JSON.parse(impulseData!);
    expect(payload).toHaveProperty('props');
  });

  test('shell contains data-component attribute', async ({ page }) => {
    await page.goto('/');
    const app = page.locator('#app');
    const componentPath = await app.getAttribute('data-component');

    expect(componentPath).toBeTruthy();
    expect(componentPath).toMatch(/^\.\//); // Starts with ./
  });

  test('all routes render valid shells', async ({ page }) => {
    const routes = [
      '/',
      '/residents',
      '/wizard',
      '/forms/insurance',
      '/modal/confirm',
      '/activity',
    ];

    for (const route of routes) {
      await page.goto(route);
      const app = page.locator('#app');
      await expect(app).toBeAttached();

      const impulseData = await app.getAttribute('data-impulse');
      expect(impulseData, `Route ${route} should have data-impulse`).toBeTruthy();
      expect(() => JSON.parse(impulseData!)).not.toThrow();
    }
  });
});

test.describe('Impulse JSON Protocol', () => {
  test('returns JSON for X-Impulse requests', async ({ request }) => {
    const response = await request.get('/', {
      headers: {
        'X-Impulse': 'true',
        Accept: 'application/json',
      },
    });

    expect(response.ok()).toBeTruthy();
    expect(response.headers()['content-type']).toContain('application/json');

    const json = await response.json();
    expect(json).toHaveProperty('props');
    expect(json).toHaveProperty('context');
  });

  test('dashboard props structure matches generated types', async ({ request }) => {
    const response = await request.get('/', {
      headers: { 'X-Impulse': 'true', Accept: 'application/json' },
    });

    const json = await response.json();
    expect(json.props).toHaveProperty('totalResidents');
    expect(json.props).toHaveProperty('totalMedications');
    expect(json.props).toHaveProperty('pendingTasks');
    expect(typeof json.props.totalResidents).toBe('number');
  });

  test('wizard props contain step data', async ({ request }) => {
    const response = await request.get('/wizard/step/3', {
      headers: { 'X-Impulse': 'true', Accept: 'application/json' },
    });

    const json = await response.json();
    expect(json.props.currentStep).toBe(3);
    expect(json.props.totalSteps).toBe(4);
    expect(json.props.steps).toHaveLength(4);
    expect(json.props.steps[2].isCurrent).toBe(true);
    expect(json.props.steps[2].title).toBe('Emergency Contact');
  });

  test('modal props contain form fields', async ({ request }) => {
    const response = await request.get('/modal/form', {
      headers: { 'X-Impulse': 'true', Accept: 'application/json' },
    });

    const json = await response.json();
    expect(json.props.isOpen).toBe(true);
    expect(json.props.modal.title).toBe('Quick Add Resident');
    expect(json.props.modal.content.formFields).toBeDefined();
    expect(json.props.modal.content.formFields.length).toBeGreaterThan(0);
  });

  test('pane props contain sections', async ({ request }) => {
    const response = await request.get('/pane/residents/1', {
      headers: { 'X-Impulse': 'true', Accept: 'application/json' },
    });

    const json = await response.json();
    expect(json.props.isOpen).toBe(true);
    expect(json.props.pane.content.sections).toBeDefined();
    expect(json.props.pane.footerActions).toBeDefined();
  });

  test('dynamic form has conditional visibility', async ({ request }) => {
    const response = await request.get('/forms/insurance', {
      headers: { 'X-Impulse': 'true', Accept: 'application/json' },
    });

    const json = await response.json();
    expect(json.props.formTitle).toBe('Insurance Application');
    expect(json.props.sections.length).toBeGreaterThan(0);

    // Check for conditional fields
    const allFields = json.props.sections.flatMap((s: any) => s.fields);
    const conditionalField = allFields.find((f: any) => f.visibleWhen !== null);
    expect(conditionalField).toBeDefined();
  });
});

test.describe('Impulse Route Generation', () => {
  test('all generated routes return 200', async ({ request }) => {
    const routes = [
      '/',
      '/residents',
      '/residents/1',
      '/residents/1/delete',
      '/residents/1/edit',
      '/residents/filter',
      '/wizard',
      '/wizard/step/1',
      '/wizard/step/2',
      '/wizard/step/3',
      '/wizard/step/4',
      '/forms/insurance',
      '/modal/confirm',
      '/modal/alert',
      '/modal/form',
      '/pane/residents/1',
      '/activity',
    ];

    for (const route of routes) {
      const response = await request.get(route);
      expect(response.ok(), `Route ${route} should return 200`).toBeTruthy();
    }
  });

  test('parameterized routes work with resident ID 1', async ({ request }) => {
    const response = await request.get('/residents/1');
    expect(response.ok()).toBeTruthy();
  });

  test('wizard step routes return 200 for steps 1-4', async ({ request }) => {
    for (const step of [1, 2, 3, 4]) {
      const response = await request.get(`/wizard/step/${step}`);
      expect(response.ok()).toBeTruthy();
    }
  });
});

test.describe('Impulse Mutations', () => {
  test('wizard next validates and advances', async ({ request }) => {
    const response = await request.post('/wizard/next', {
      data: {
        currentStep: 1,
        formData: {
          firstName: 'John',
          lastName: 'Doe',
          dateOfBirth: '1990-01-15',
        },
      },
    });

    expect(response.ok()).toBeTruthy();
    const json = await response.json();
    expect(json.success).toBe(true);
    expect(json.nextStep).toBe(2);
  });

  test('wizard next returns validation errors', async ({ request }) => {
    const response = await request.post('/wizard/next', {
      data: {
        currentStep: 1,
        formData: {
          firstName: '',
          lastName: '',
          dateOfBirth: null,
        },
      },
    });

    expect(response.ok()).toBeTruthy();
    const json = await response.json();
    expect(json.success).toBe(false);
    expect(json.validationErrors).toHaveProperty('firstName');
    expect(json.validationErrors).toHaveProperty('lastName');
    expect(json.validationErrors).toHaveProperty('dateOfBirth');
  });

  test('wizard back navigation works', async ({ request }) => {
    const response = await request.post('/wizard/back', {
      data: { currentStep: 3 },
    });

    expect(response.ok()).toBeTruthy();
    const json = await response.json();
    expect(json.success).toBe(true);
    expect(json.nextStep).toBe(2);
  });

  test('medications API returns medications data', async ({ request }) => {
    const response = await request.get('/residents/1/medications');
    expect(response.ok()).toBeTruthy();

    const json = await response.json();
    expect(json).toHaveProperty('medications');
    expect(Array.isArray(json.medications)).toBe(true);
  });
});

test.describe('Impulse Complex Props', () => {
  test('activity feed has timestamped items', async ({ request }) => {
    const response = await request.get('/activity', {
      headers: { 'X-Impulse': 'true', Accept: 'application/json' },
    });

    const json = await response.json();
    expect(json.props.activities.length).toBeGreaterThan(0);

    const firstActivity = json.props.activities[0];
    expect(firstActivity).toHaveProperty('id');
    expect(firstActivity).toHaveProperty('type');
    expect(firstActivity).toHaveProperty('description');
    expect(firstActivity).toHaveProperty('timestamp');
  });

  test('filter pane has filter groups', async ({ request }) => {
    const response = await request.get('/residents/filter', {
      headers: { 'X-Impulse': 'true', Accept: 'application/json' },
    });

    const json = await response.json();
    expect(json.props.filterGroups.length).toBeGreaterThan(0);

    const checkboxFilter = json.props.filterGroups.find((g: any) => g.type === 0);
    expect(checkboxFilter).toBeDefined();
    expect(checkboxFilter.options.length).toBeGreaterThan(0);
  });

  test('edit modal has available rooms', async ({ request }) => {
    const response = await request.get('/residents/1/edit', {
      headers: { 'X-Impulse': 'true', Accept: 'application/json' },
    });

    const json = await response.json();
    expect(json.props.availableRooms).toBeDefined();
    expect(json.props.availableRooms.length).toBeGreaterThan(0);
    expect(json.props.availableRooms[0]).toHaveProperty('roomNumber');
    expect(json.props.availableRooms[0]).toHaveProperty('buildingName');
    expect(json.props.availableRooms[0]).toHaveProperty('isAvailable');
  });
});

test.describe('Screenshot Evidence Gallery', () => {
  test('capture all route screenshots', async ({ page }) => {
    const routes = [
      { path: '/', name: 'dashboard' },
      { path: '/residents', name: 'residents-list' },
      { path: '/residents/1', name: 'resident-detail' },
      { path: '/wizard', name: 'wizard-step1' },
      { path: '/wizard/step/2', name: 'wizard-step2' },
      { path: '/wizard/step/3', name: 'wizard-step3' },
      { path: '/wizard/step/4', name: 'wizard-step4' },
      { path: '/forms/insurance', name: 'dynamic-form' },
      { path: '/modal/confirm', name: 'modal-confirm' },
      { path: '/modal/alert', name: 'modal-alert' },
      { path: '/modal/form', name: 'modal-form' },
      { path: '/pane/residents/1', name: 'pane-detail' },
      { path: '/activity', name: 'activity-feed' },
      { path: '/residents/filter', name: 'filter-pane' },
      { path: '/residents/1/delete', name: 'delete-modal' },
      { path: '/residents/1/edit', name: 'edit-modal' },
    ];

    for (const { path, name } of routes) {
      await page.goto(path);
      await expect(page.locator('#app')).toBeAttached();
      await page.screenshot({
        path: `screenshots/impulse-${name}.png`,
        fullPage: true,
      });
    }
  });
});
