import { test, expect } from '@playwright/test';

test.describe('Full Application Flow - Complex Scenarios', () => {
  test('homepage dashboard renders', async ({ page }) => {
    await page.goto('/');

    // Verify dashboard content
    await expect(page.locator('#app')).toBeVisible();
    await expect(page.locator('h1')).toContainText('Dashboard');

    await page.screenshot({ path: 'screenshots/app-dashboard.png', fullPage: true });
  });

  test('navigate to residents list', async ({ page }) => {
    await page.goto('/residents');

    // Verify residents list
    await expect(page.locator('#app')).toBeVisible();

    await page.screenshot({ path: 'screenshots/app-residents-list.png', fullPage: true });
  });

  test('navigate to resident detail', async ({ page }) => {
    await page.goto('/residents/1');

    // Verify resident detail
    await expect(page.locator('#app')).toBeVisible();

    await page.screenshot({ path: 'screenshots/app-resident-detail.png', fullPage: true });
  });

  test('complete wizard flow', async ({ page }) => {
    // Step 1
    await page.goto('/wizard');
    await page.screenshot({ path: 'screenshots/flow-wizard-step1.png', fullPage: true });

    // Step 2
    await page.goto('/wizard/step/2');
    await page.screenshot({ path: 'screenshots/flow-wizard-step2.png', fullPage: true });

    // Step 3
    await page.goto('/wizard/step/3');
    await page.screenshot({ path: 'screenshots/flow-wizard-step3.png', fullPage: true });

    // Step 4
    await page.goto('/wizard/step/4');
    await page.screenshot({ path: 'screenshots/flow-wizard-step4.png', fullPage: true });
  });

  test('modal interactions', async ({ page }) => {
    // Confirm modal
    await page.goto('/modal/confirm');
    await page.screenshot({ path: 'screenshots/flow-modal-confirm.png', fullPage: true });

    // Alert modal
    await page.goto('/modal/alert');
    await page.screenshot({ path: 'screenshots/flow-modal-alert.png', fullPage: true });

    // Form modal
    await page.goto('/modal/form');
    await page.screenshot({ path: 'screenshots/flow-modal-form.png', fullPage: true });
  });

  test('pane interactions', async ({ page }) => {
    // Resident pane
    await page.goto('/pane/residents/1');
    await page.screenshot({ path: 'screenshots/flow-pane-resident.png', fullPage: true });

    // Filter pane
    await page.goto('/residents/filter');
    await page.screenshot({ path: 'screenshots/flow-pane-filter.png', fullPage: true });

    // Activity feed
    await page.goto('/activity');
    await page.screenshot({ path: 'screenshots/flow-pane-activity.png', fullPage: true });
  });

  test('dynamic form with all insurance types', async ({ page }) => {
    await page.goto('/forms/insurance');
    await page.screenshot({ path: 'screenshots/flow-form-initial.png', fullPage: true });

    // Auto insurance
    await page.locator('input[name="insuranceType"][value="auto"]').check();
    await page.waitForTimeout(300);
    await page.screenshot({ path: 'screenshots/flow-form-auto.png', fullPage: true });

    // Home insurance
    await page.locator('input[name="insuranceType"][value="home"]').check();
    await page.waitForTimeout(300);
    await page.screenshot({ path: 'screenshots/flow-form-home.png', fullPage: true });

    // Life insurance
    await page.locator('input[name="insuranceType"][value="life"]').check();
    await page.waitForTimeout(300);
    await page.screenshot({ path: 'screenshots/flow-form-life.png', fullPage: true });
  });

  test('server renders valid JSON payload', async ({ request }) => {
    const pages = ['/', '/residents', '/wizard', '/forms/insurance', '/modal/confirm'];

    for (const url of pages) {
      const response = await request.get(url, {
        headers: {
          'X-Impulse': 'true',
          Accept: 'application/json',
        },
      });

      expect(response.ok()).toBeTruthy();
      const contentType = response.headers()['content-type'];
      expect(contentType).toContain('application/json');

      const json = await response.json();
      expect(json).toHaveProperty('url');
      expect(json).toHaveProperty('props');
      expect(json).toHaveProperty('version');
    }
  });

  test('shell contains hydration attributes', async ({ page }) => {
    const pages = ['/', '/residents', '/wizard', '/forms/insurance'];

    for (const url of pages) {
      await page.goto(url);

      const app = page.locator('#app');
      await expect(app).toBeVisible();

      // Should have data-impulse attribute with JSON
      const impulseData = await app.getAttribute('data-impulse');
      expect(impulseData).toBeTruthy();
      expect(() => JSON.parse(impulseData!)).not.toThrow();

      // Should have data-component attribute
      const componentPath = await app.getAttribute('data-component');
      expect(componentPath).toBeTruthy();
      expect(componentPath).toMatch(/^\.\//);
    }
  });

  test('all routes return 200', async ({ request }) => {
    const routes = [
      '/',
      '/residents',
      '/residents/1',
      '/wizard',
      '/wizard/step/2',
      '/wizard/step/3',
      '/wizard/step/4',
      '/forms/insurance',
      '/residents/1/delete',
      '/residents/1/edit',
      '/pane/residents/1',
      '/residents/filter',
      '/activity',
      '/modal/confirm',
      '/modal/alert',
      '/modal/form',
    ];

    for (const route of routes) {
      const response = await request.get(route);
      expect(response.ok(), `Route ${route} should return 200`).toBeTruthy();
    }
  });

  test('API endpoints work', async ({ request }) => {
    // GET medications
    const medsResponse = await request.get('/residents/1/medications');
    expect(medsResponse.ok()).toBeTruthy();

    // POST wizard next (without data)
    const wizardResponse = await request.post('/wizard/next', {
      data: {
        currentStep: 1,
        formData: {
          firstName: 'Test',
          lastName: 'User',
          dateOfBirth: '1990-01-01',
        },
      },
    });
    expect(wizardResponse.ok()).toBeTruthy();
  });
});

test.describe('Screenshot Gallery - All Components', () => {
  test('capture all component screenshots', async ({ page }) => {
    const screenshotTargets = [
      { url: '/', name: 'dashboard' },
      { url: '/residents', name: 'residents-list' },
      { url: '/residents/1', name: 'resident-detail' },
      { url: '/wizard', name: 'wizard-step1' },
      { url: '/wizard/step/2', name: 'wizard-step2' },
      { url: '/wizard/step/3', name: 'wizard-step3' },
      { url: '/wizard/step/4', name: 'wizard-step4' },
      { url: '/forms/insurance', name: 'insurance-form' },
      { url: '/residents/1/delete', name: 'delete-modal' },
      { url: '/residents/1/edit', name: 'edit-modal' },
      { url: '/pane/residents/1', name: 'resident-pane' },
      { url: '/residents/filter', name: 'filter-pane' },
      { url: '/activity', name: 'activity-feed' },
      { url: '/modal/confirm', name: 'confirm-modal' },
      { url: '/modal/alert', name: 'alert-modal' },
      { url: '/modal/form', name: 'form-modal' },
    ];

    for (const target of screenshotTargets) {
      await page.goto(target.url);
      await page.waitForLoadState('networkidle');
      await page.screenshot({
        path: `screenshots/gallery-${target.name}.png`,
        fullPage: true,
      });
    }
  });
});
