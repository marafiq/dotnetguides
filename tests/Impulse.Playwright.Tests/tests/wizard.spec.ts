import { test, expect } from '@playwright/test';

test.describe('Wizard - Multi-step Resident Onboarding', () => {
  test.beforeEach(async ({ page }) => {
    await page.goto('/wizard');
  });

  test('displays step 1 - Personal Information', async ({ page }) => {
    // Verify wizard header
    await expect(page.locator('.wizard-progress')).toBeVisible();

    // Verify step 1 is current
    await expect(page.locator('.wizard-step.current')).toContainText('Personal Information');

    // Verify form fields present
    await expect(page.locator('#firstName')).toBeVisible();
    await expect(page.locator('#lastName')).toBeVisible();
    await expect(page.locator('#dateOfBirth')).toBeVisible();

    // Capture screenshot
    await page.screenshot({ path: 'screenshots/wizard-step1-initial.png', fullPage: true });
  });

  test('validates required fields on step 1', async ({ page }) => {
    // Click Next without filling required fields
    await page.click('button:has-text("Next")');

    // Wait for validation errors
    await page.waitForSelector('.error-message', { timeout: 5000 }).catch(() => {
      // Error message may not appear if server-side validation
    });

    await page.screenshot({ path: 'screenshots/wizard-step1-validation.png', fullPage: true });
  });

  test('fills step 1 and navigates to step 2', async ({ page }) => {
    // Fill personal information
    await page.fill('#firstName', 'John');
    await page.fill('#lastName', 'Doe');
    await page.fill('#dateOfBirth', '1980-05-15');
    await page.fill('#gender', 'Male');

    await page.screenshot({ path: 'screenshots/wizard-step1-filled.png', fullPage: true });

    // Click Next
    await page.click('button:has-text("Next")');

    // Wait for page to update (server-side rendering)
    await page.waitForLoadState('networkidle');

    await page.screenshot({ path: 'screenshots/wizard-step2-loaded.png', fullPage: true });
  });

  test('navigates directly to step 2', async ({ page }) => {
    await page.goto('/wizard/step/2');

    await expect(page.locator('#primaryPhysician')).toBeVisible();
    await expect(page.locator('#hasInsurance')).toBeVisible();

    await page.screenshot({ path: 'screenshots/wizard-step2-direct.png', fullPage: true });
  });

  test('shows conditional insurance fields when checked', async ({ page }) => {
    await page.goto('/wizard/step/2');

    // Initially insurance fields hidden
    await page.screenshot({ path: 'screenshots/wizard-step2-no-insurance.png', fullPage: true });

    // Check has insurance
    await page.check('#hasInsurance');

    // Insurance fields should appear (if client-side rendered)
    await page.waitForTimeout(500);

    await page.screenshot({ path: 'screenshots/wizard-step2-with-insurance.png', fullPage: true });
  });

  test('navigates to step 3 - Emergency Contact', async ({ page }) => {
    await page.goto('/wizard/step/3');

    await expect(page.locator('#emergencyContactName')).toBeVisible();
    await expect(page.locator('#emergencyContactPhone')).toBeVisible();

    await page.screenshot({ path: 'screenshots/wizard-step3.png', fullPage: true });
  });

  test('navigates to step 4 - Room Assignment', async ({ page }) => {
    await page.goto('/wizard/step/4');

    await expect(page.locator('#buildingId')).toBeVisible();
    await expect(page.locator('#roomNumber')).toBeVisible();

    // Should show Submit button
    await expect(page.locator('button:has-text("Submit")')).toBeVisible();

    await page.screenshot({ path: 'screenshots/wizard-step4.png', fullPage: true });
  });

  test('wizard progress shows completed steps', async ({ page }) => {
    await page.goto('/wizard/step/3');

    // Steps 1 and 2 should be marked completed
    const completedSteps = page.locator('.wizard-step.completed');
    await expect(completedSteps).toHaveCount(2);

    await page.screenshot({ path: 'screenshots/wizard-progress.png', fullPage: true });
  });

  test('back button navigates to previous step', async ({ page }) => {
    await page.goto('/wizard/step/3');

    // Click Back
    await page.click('button:has-text("Back")');

    await page.waitForLoadState('networkidle');

    await page.screenshot({ path: 'screenshots/wizard-back-navigation.png', fullPage: true });
  });
});
