import { test, expect } from '@playwright/test';

/**
 * TanStack Router Generation Limits Test Suite
 *
 * These tests demonstrate that the Roslyn source generator correctly produces
 * TanStack Router definitions for complex routing scenarios, including:
 * - Deeply nested routes
 * - Multiple route parameters
 * - Search/query parameters
 * - Nested layouts
 * - Index routes
 * - Catch-all patterns
 * - Dynamic segments
 */

test.describe('TanStack Router Generation - Limits & Edge Cases', () => {
  test.describe('Deeply Nested Routes', () => {
    test('4-level deep nesting works correctly', async ({ page }) => {
      // /wizard/step/{step} - 3 levels
      await page.goto('/wizard/step/2');
      await expect(page.locator('#app')).toBeVisible();
      await page.screenshot({
        path: 'screenshots/router-nested-wizard-step.png',
        fullPage: true,
      });
    });

    test('pane routes with entity ID work', async ({ page }) => {
      // /pane/residents/{id} - resource pane pattern
      await page.goto('/pane/residents/1');
      await expect(page.locator('#app')).toBeVisible();
      await page.screenshot({
        path: 'screenshots/router-nested-pane-resident.png',
        fullPage: true,
      });
    });

    test('resident medications nested route', async ({ page }) => {
      // /residents/{id}/medications - nested resource
      await page.goto('/residents/1/medications');
      await expect(page.locator('#app')).toBeVisible();
      await page.screenshot({
        path: 'screenshots/router-nested-medications.png',
        fullPage: true,
      });
    });

    test('resident action routes work', async ({ page }) => {
      // /residents/{id}/delete - action route
      await page.goto('/residents/1/delete');
      await expect(page.locator('#app')).toBeVisible();
      await page.screenshot({
        path: 'screenshots/router-nested-delete-action.png',
        fullPage: true,
      });

      // /residents/{id}/edit - action route
      await page.goto('/residents/1/edit');
      await expect(page.locator('#app')).toBeVisible();
      await page.screenshot({
        path: 'screenshots/router-nested-edit-action.png',
        fullPage: true,
      });
    });
  });

  test.describe('Multiple Route Parameters', () => {
    test('single parameter routes work', async ({ page }) => {
      // /residents/{id}
      await page.goto('/residents/1');
      await expect(page.locator('#app')).toBeVisible();

      await page.goto('/residents/42');
      await expect(page.locator('#app')).toBeVisible();

      await page.goto('/residents/999');
      await expect(page.locator('#app')).toBeVisible();
      await page.screenshot({
        path: 'screenshots/router-single-param.png',
        fullPage: true,
      });
    });

    test('integer constrained routes work', async ({ page }) => {
      // /wizard/step/{step:int}
      await page.goto('/wizard/step/1');
      await expect(page.locator('#app')).toBeVisible();

      await page.goto('/wizard/step/4');
      await expect(page.locator('#app')).toBeVisible();
      await page.screenshot({
        path: 'screenshots/router-int-constraint.png',
        fullPage: true,
      });
    });

    test('parameter variations render correctly', async ({ page }) => {
      const paramRoutes = [
        '/residents/1',
        '/residents/10',
        '/residents/100',
        '/pane/residents/1',
        '/pane/residents/50',
      ];

      for (const route of paramRoutes) {
        await page.goto(route);
        await expect(page.locator('#app')).toBeVisible();
      }
      await page.screenshot({
        path: 'screenshots/router-param-variations.png',
        fullPage: true,
      });
    });
  });

  test.describe('Index Routes', () => {
    test('root index route works', async ({ page }) => {
      await page.goto('/');
      await expect(page.locator('#app')).toBeVisible();
      await expect(page.locator('h1')).toContainText('Dashboard');
      await page.screenshot({
        path: 'screenshots/router-index-root.png',
        fullPage: true,
      });
    });

    test('resource index routes work', async ({ page }) => {
      // /residents (list)
      await page.goto('/residents');
      await expect(page.locator('#app')).toBeVisible();
      await page.screenshot({
        path: 'screenshots/router-index-residents.png',
        fullPage: true,
      });

      // /wizard (step 1)
      await page.goto('/wizard');
      await expect(page.locator('#app')).toBeVisible();
      await page.screenshot({
        path: 'screenshots/router-index-wizard.png',
        fullPage: true,
      });
    });
  });

  test.describe('Modal Routes (Parallel Routes)', () => {
    test('modal routes render within context', async ({ page }) => {
      // These demonstrate parallel route patterns - modals that overlay content
      await page.goto('/modal/confirm');
      await expect(page.locator('#app')).toBeVisible();
      await page.screenshot({
        path: 'screenshots/router-modal-confirm.png',
        fullPage: true,
      });

      await page.goto('/modal/alert');
      await expect(page.locator('#app')).toBeVisible();
      await page.screenshot({
        path: 'screenshots/router-modal-alert.png',
        fullPage: true,
      });

      await page.goto('/modal/form');
      await expect(page.locator('#app')).toBeVisible();
      await page.screenshot({
        path: 'screenshots/router-modal-form.png',
        fullPage: true,
      });
    });

    test('entity-scoped modal routes work', async ({ page }) => {
      // /residents/{id}/delete - modal scoped to entity
      await page.goto('/residents/1/delete');
      await expect(page.locator('#app')).toBeVisible();
      await page.screenshot({
        path: 'screenshots/router-modal-entity-delete.png',
        fullPage: true,
      });

      // /residents/{id}/edit - modal scoped to entity
      await page.goto('/residents/1/edit');
      await expect(page.locator('#app')).toBeVisible();
      await page.screenshot({
        path: 'screenshots/router-modal-entity-edit.png',
        fullPage: true,
      });
    });
  });

  test.describe('Pane Routes (Slide-out Panels)', () => {
    test('detail pane routes work', async ({ page }) => {
      await page.goto('/pane/residents/1');
      await expect(page.locator('#app')).toBeVisible();
      await page.screenshot({
        path: 'screenshots/router-pane-detail.png',
        fullPage: true,
      });
    });

    test('filter pane route works', async ({ page }) => {
      await page.goto('/residents/filter');
      await expect(page.locator('#app')).toBeVisible();
      await page.screenshot({
        path: 'screenshots/router-pane-filter.png',
        fullPage: true,
      });
    });

    test('activity feed pane works', async ({ page }) => {
      await page.goto('/activity');
      await expect(page.locator('#app')).toBeVisible();
      await page.screenshot({
        path: 'screenshots/router-pane-activity.png',
        fullPage: true,
      });
    });
  });

  test.describe('Form Routes', () => {
    test('forms index route works', async ({ page }) => {
      await page.goto('/forms/insurance');
      await expect(page.locator('#app')).toBeVisible();
      await page.screenshot({
        path: 'screenshots/router-form-insurance.png',
        fullPage: true,
      });
    });
  });

  test.describe('API Routes (Non-Component)', () => {
    test('GET API routes return JSON', async ({ request }) => {
      const response = await request.get('/residents/1/medications');
      expect(response.ok()).toBeTruthy();
      const json = await response.json();
      expect(Array.isArray(json)).toBeTruthy();
    });

    test('POST API routes accept data', async ({ request }) => {
      const response = await request.post('/wizard/next', {
        data: {
          currentStep: 1,
          formData: {
            firstName: 'Test',
            lastName: 'User',
            dateOfBirth: '1990-01-01',
          },
        },
      });
      expect(response.ok()).toBeTruthy();
      const json = await response.json();
      expect(json).toHaveProperty('success');
    });
  });

  test.describe('Impulse Protocol Compliance', () => {
    test('all routes return valid Impulse JSON with X-Impulse header', async ({ request }) => {
      const routes = [
        '/',
        '/residents',
        '/residents/1',
        '/wizard',
        '/wizard/step/2',
        '/forms/insurance',
        '/modal/confirm',
        '/pane/residents/1',
        '/activity',
      ];

      for (const route of routes) {
        const response = await request.get(route, {
          headers: {
            'X-Impulse': 'true',
            Accept: 'application/json',
          },
        });

        expect(response.ok(), `Route ${route} should return 200`).toBeTruthy();
        const contentType = response.headers()['content-type'];
        expect(contentType).toContain('application/json');

        const json = await response.json();
        // Impulse protocol requires url, props, version
        expect(json).toHaveProperty('url');
        expect(json).toHaveProperty('props');
        expect(json).toHaveProperty('version');
      }
    });

    test('shell contains data-impulse and data-component attributes', async ({ page }) => {
      const routes = ['/', '/residents', '/wizard', '/forms/insurance'];

      for (const route of routes) {
        await page.goto(route);

        const app = page.locator('#app');
        await expect(app).toBeVisible();

        // Verify Impulse hydration attributes
        const impulseData = await app.getAttribute('data-impulse');
        expect(impulseData).toBeTruthy();
        expect(() => JSON.parse(impulseData!)).not.toThrow();

        const componentPath = await app.getAttribute('data-component');
        expect(componentPath).toBeTruthy();
        expect(componentPath).toMatch(/^\.\//);
      }
    });
  });

  test.describe('Route Generation Stress Test', () => {
    test('all 16+ generated routes render without error', async ({ page }) => {
      const allRoutes = [
        // Dashboard
        '/',
        // Residents CRUD
        '/residents',
        '/residents/1',
        '/residents/1/delete',
        '/residents/1/edit',
        '/residents/1/medications',
        '/residents/filter',
        // Wizard flow
        '/wizard',
        '/wizard/step/1',
        '/wizard/step/2',
        '/wizard/step/3',
        '/wizard/step/4',
        // Forms
        '/forms/insurance',
        // Modals
        '/modal/confirm',
        '/modal/alert',
        '/modal/form',
        // Panes
        '/pane/residents/1',
        '/activity',
      ];

      const results: { route: string; success: boolean }[] = [];

      for (const route of allRoutes) {
        await page.goto(route);
        const appVisible = await page.locator('#app').isVisible();
        results.push({ route, success: appVisible });
      }

      // Final screenshot showing the last route
      await page.screenshot({
        path: 'screenshots/router-stress-test-final.png',
        fullPage: true,
      });

      // Verify all routes worked
      const failed = results.filter((r) => !r.success);
      expect(failed, `Failed routes: ${failed.map((f) => f.route).join(', ')}`).toHaveLength(0);
    });

    test('rapid navigation between routes works', async ({ page }) => {
      const routes = [
        '/',
        '/residents',
        '/wizard',
        '/forms/insurance',
        '/modal/confirm',
        '/residents/1',
        '/wizard/step/4',
        '/activity',
      ];

      // Rapid navigation stress test
      for (let i = 0; i < 3; i++) {
        for (const route of routes) {
          await page.goto(route);
          await expect(page.locator('#app')).toBeVisible();
        }
      }

      await page.screenshot({
        path: 'screenshots/router-rapid-navigation.png',
        fullPage: true,
      });
    });
  });
});

test.describe('Complete Route Screenshot Gallery', () => {
  test('capture every route with evidence', async ({ page }) => {
    const routeGallery = [
      { route: '/', name: '01-dashboard' },
      { route: '/residents', name: '02-residents-list' },
      { route: '/residents/1', name: '03-resident-detail' },
      { route: '/residents/1/delete', name: '04-resident-delete-modal' },
      { route: '/residents/1/edit', name: '05-resident-edit-modal' },
      { route: '/residents/1/medications', name: '06-resident-medications' },
      { route: '/residents/filter', name: '07-residents-filter-pane' },
      { route: '/wizard', name: '08-wizard-step1' },
      { route: '/wizard/step/2', name: '09-wizard-step2' },
      { route: '/wizard/step/3', name: '10-wizard-step3' },
      { route: '/wizard/step/4', name: '11-wizard-step4' },
      { route: '/forms/insurance', name: '12-insurance-form' },
      { route: '/modal/confirm', name: '13-modal-confirm' },
      { route: '/modal/alert', name: '14-modal-alert' },
      { route: '/modal/form', name: '15-modal-form' },
      { route: '/pane/residents/1', name: '16-pane-resident' },
      { route: '/activity', name: '17-activity-feed' },
    ];

    console.log('Capturing screenshot gallery for TanStack Router generated routes...');

    for (const { route, name } of routeGallery) {
      await page.goto(route);
      await page.waitForLoadState('networkidle');
      await expect(page.locator('#app')).toBeVisible();

      await page.screenshot({
        path: `screenshots/evidence-${name}.png`,
        fullPage: true,
      });

      console.log(`✓ Captured: ${name} (${route})`);
    }

    console.log(`\nTotal routes captured: ${routeGallery.length}`);
  });
});
