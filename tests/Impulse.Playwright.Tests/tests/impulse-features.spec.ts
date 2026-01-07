import { test, expect } from '@playwright/test';

/**
 * Real Impulse Feature Tests
 * Tests actual UI rendering, form validation, mutations, and user interactions
 */

test.describe('Dashboard - Visible UI', () => {
  test('renders dashboard with visible stats', async ({ page }) => {
    await page.goto('/');

    // Wait for React hydration
    await page.waitForSelector('.dashboard', { timeout: 5000 });

    // Verify visible content
    await expect(page.locator('h1')).toContainText('Dashboard');
    await expect(page.locator('.stat-card')).toHaveCount(3);
    await expect(page.locator('.stat-value').first()).toBeVisible();

    await page.screenshot({ path: 'screenshots/dashboard-visible.png', fullPage: true });
  });
});

test.describe('Wizard - Form Validation', () => {
  test('displays wizard step 1 with form fields', async ({ page }) => {
    await page.goto('/wizard');

    await page.waitForSelector('.wizard', { timeout: 5000 });

    // Verify wizard UI
    await expect(page.locator('.wizard-progress')).toBeVisible();
    await expect(page.locator('.wizard-step').first()).toHaveClass(/current/);

    // Verify form fields exist
    await expect(page.locator('input[name="firstName"]')).toBeVisible();
    await expect(page.locator('input[name="lastName"]')).toBeVisible();
    await expect(page.locator('input[name="dateOfBirth"]')).toBeVisible();

    await page.screenshot({ path: 'screenshots/wizard-step1.png', fullPage: true });
  });

  test('shows validation errors on empty submit', async ({ page }) => {
    await page.goto('/wizard');
    await page.waitForSelector('.wizard', { timeout: 5000 });

    // Click Next without filling form
    await page.click('button:has-text("Next")');

    // Wait for validation errors
    await page.waitForSelector('.error-message', { timeout: 5000 });

    // Verify errors are visible
    await expect(page.locator('.error-message')).toHaveCount(3);
    await expect(page.locator('.form-field.has-error')).toHaveCount(3);

    await page.screenshot({ path: 'screenshots/wizard-validation-errors.png', fullPage: true });
  });

  test('advances to step 2 with valid data', async ({ page }) => {
    await page.goto('/wizard');
    await page.waitForSelector('.wizard', { timeout: 5000 });

    // Fill form
    await page.fill('input[name="firstName"]', 'John');
    await page.fill('input[name="lastName"]', 'Doe');
    await page.fill('input[name="dateOfBirth"]', '1990-01-15');

    await page.screenshot({ path: 'screenshots/wizard-filled.png', fullPage: true });

    // Submit
    await page.click('button:has-text("Next")');

    // Wait for navigation or step change
    await page.waitForURL('**/wizard/step/2', { timeout: 5000 });

    await page.screenshot({ path: 'screenshots/wizard-step2.png', fullPage: true });
  });

  test('wizard step 2 shows medical fields', async ({ page }) => {
    await page.goto('/wizard/step/2');
    await page.waitForSelector('.wizard', { timeout: 5000 });

    // Verify step 2 content
    await expect(page.locator('input[name="primaryPhysician"]')).toBeVisible();
    await expect(page.locator('input[name="hasInsurance"]')).toBeVisible();

    await page.screenshot({ path: 'screenshots/wizard-medical.png', fullPage: true });
  });

  test('wizard step 3 shows emergency contact', async ({ page }) => {
    await page.goto('/wizard/step/3');
    await page.waitForSelector('.wizard', { timeout: 5000 });

    await expect(page.locator('input[name="emergencyContactName"]')).toBeVisible();
    await expect(page.locator('input[name="emergencyContactPhone"]')).toBeVisible();

    await page.screenshot({ path: 'screenshots/wizard-emergency.png', fullPage: true });
  });

  test('wizard step 4 shows review', async ({ page }) => {
    await page.goto('/wizard/step/4');
    await page.waitForSelector('.wizard', { timeout: 5000 });

    await expect(page.locator('.review-section')).toBeVisible();
    await expect(page.locator('button:has-text("Submit")')).toBeVisible();

    await page.screenshot({ path: 'screenshots/wizard-review.png', fullPage: true });
  });
});

test.describe('Residents List', () => {
  test('displays residents table', async ({ page }) => {
    await page.goto('/residents');
    await page.waitForSelector('.residents-list', { timeout: 5000 });

    await expect(page.locator('h1')).toContainText('Residents');
    await expect(page.locator('table')).toBeVisible();
    await expect(page.locator('tbody tr')).toHaveCount(5);

    await page.screenshot({ path: 'screenshots/residents-list.png', fullPage: true });
  });

  test('resident links navigate to detail', async ({ page }) => {
    await page.goto('/residents');
    await page.waitForSelector('.residents-list', { timeout: 5000 });

    // Click first resident
    await page.click('tbody tr:first-child a');

    await page.waitForURL('**/residents/1', { timeout: 5000 });
    await page.screenshot({ path: 'screenshots/resident-detail.png', fullPage: true });
  });
});

test.describe('Dynamic Form - Insurance Application', () => {
  test('renders form with sections', async ({ page }) => {
    await page.goto('/forms/insurance');
    await page.waitForSelector('.dynamic-form', { timeout: 5000 });

    await expect(page.locator('h1')).toContainText('Insurance Application');
    await expect(page.locator('.form-section')).toHaveCount(4);

    await page.screenshot({ path: 'screenshots/insurance-form.png', fullPage: true });
  });

  test('conditional fields appear based on selection', async ({ page }) => {
    await page.goto('/forms/insurance');
    await page.waitForSelector('.dynamic-form', { timeout: 5000 });

    // Initially auto insurance fields hidden
    const autoSection = page.locator('[data-section="auto"]');

    // Select auto insurance
    await page.check('input[value="auto"]');

    // Auto fields should appear
    await expect(page.locator('input[name="vehicleMake"]')).toBeVisible();

    await page.screenshot({ path: 'screenshots/insurance-conditional.png', fullPage: true });
  });
});

test.describe('Modal - Delete Confirmation', () => {
  test('displays delete modal', async ({ page }) => {
    await page.goto('/residents/1/delete');
    await page.waitForSelector('.modal', { timeout: 5000 });

    await expect(page.locator('.modal-header h2')).toContainText('Delete');
    await expect(page.locator('.btn-danger')).toBeVisible();
    await expect(page.locator('.btn-secondary')).toBeVisible();

    await page.screenshot({ path: 'screenshots/delete-modal.png', fullPage: true });
  });
});

test.describe('Pane - Resident Detail', () => {
  test('displays detail pane with sections', async ({ page }) => {
    await page.goto('/pane/residents/1');
    await page.waitForSelector('.pane', { timeout: 5000 });

    await expect(page.locator('.pane-header')).toBeVisible();
    await expect(page.locator('.pane-section')).toHaveCount(3);

    await page.screenshot({ path: 'screenshots/resident-pane.png', fullPage: true });
  });
});

test.describe('Activity Feed', () => {
  test('displays activity items', async ({ page }) => {
    await page.goto('/activity');
    await page.waitForSelector('.activity-feed', { timeout: 5000 });

    await expect(page.locator('.activity-item')).toHaveCount(5);
    await expect(page.locator('.activity-time').first()).toBeVisible();

    await page.screenshot({ path: 'screenshots/activity-feed.png', fullPage: true });
  });
});

test.describe('Mutations - Server Validation', () => {
  test('wizard mutation returns validation errors', async ({ request }) => {
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
    expect(json.validationErrors).toBeDefined();
    expect(json.validationErrors.firstName).toBeDefined();
    expect(json.validationErrors.lastName).toBeDefined();
  });

  test('wizard mutation succeeds with valid data', async ({ request }) => {
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
});
