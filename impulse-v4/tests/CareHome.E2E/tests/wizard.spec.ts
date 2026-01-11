import { test, expect } from '@playwright/test';

/**
 * Impulse v4 - Admission Wizard E2E Tests
 * Demonstrates:
 * - Server-driven wizard configuration
 * - Generated types from C# (in generated/ folder)
 * - Zod validation (client-side)
 * - Server-side ProblemDetails validation
 * - S2 Form + validationErrors per step
 */

test.describe('Admission Wizard Feature', () => {
  test.beforeEach(async ({ page }) => {
    await page.goto('/admission/wizard');
    await page.waitForSelector('[data-testid="admission-wizard"]');
  });

  test('Wizard renders with 5 steps and progress indicator', async ({ page }) => {
    // Verify wizard header
    await expect(page.getByRole('heading', { level: 1 })).toContainText('Resident Admission');

    // Verify step indicators
    const stepIndicators = page.locator('.wizard-step-indicator');
    await expect(stepIndicators).toHaveCount(5);

    // Verify first step is active
    await expect(page.locator('[data-testid="basic-info-step"]')).toBeVisible();

    await page.screenshot({ path: 'test-results/screenshots/wizard-step1-initial.png' });
  });

  test('Step 1: Basic Info validation shows errors for empty fields', async ({ page }) => {
    // Click Next without filling any fields
    await page.getByTestId('next-button').click();

    // Wait for validation
    await page.waitForTimeout(500);

    // Should show validation errors for required fields
    await page.screenshot({ path: 'test-results/screenshots/wizard-step1-validation.png' });
  });

  test('Step 1: Basic Info can be filled and advances to Step 2', async ({ page }) => {
    // Fill out basic info
    await page.getByTestId('input-firstName').fill('John');
    await page.getByTestId('input-lastName').fill('Smith');
    await page.getByTestId('input-dateOfBirth').fill('1950-05-15');

    await page.screenshot({ path: 'test-results/screenshots/wizard-step1-filled.png' });

    // Click Next
    await page.getByTestId('next-button').click();
    await page.waitForTimeout(500);

    // Verify we're on Step 2
    await expect(page.getByRole('heading', { level: 2 })).toContainText('Medical History');
    await expect(page.locator('[data-testid="medical-history-step"]')).toBeVisible();

    await page.screenshot({ path: 'test-results/screenshots/wizard-step2-initial.png' });
  });

  test('Step 2: Medical History can be filled and advances', async ({ page }) => {
    // Navigate to step 2
    await page.getByTestId('input-firstName').fill('John');
    await page.getByTestId('input-lastName').fill('Smith');
    await page.getByTestId('input-dateOfBirth').fill('1950-05-15');
    await page.getByTestId('next-button').click();
    await page.waitForTimeout(500);

    // Fill medical history
    await page.getByTestId('input-conditions').fill('Diabetes\nHypertension');
    await page.getByTestId('input-medications').fill('Metformin\nLisinopril');

    await page.screenshot({ path: 'test-results/screenshots/wizard-step2-filled.png' });

    // Continue to step 3
    await page.getByTestId('next-button').click();
    await page.waitForTimeout(500);

    await expect(page.getByRole('heading', { level: 2 })).toContainText('Care Preferences');
    await page.screenshot({ path: 'test-results/screenshots/wizard-step3-initial.png' });
  });

  test('Step 3: Care Preferences with Picker and Checkboxes', async ({ page }) => {
    // Navigate to step 3
    await page.getByTestId('input-firstName').fill('John');
    await page.getByTestId('input-lastName').fill('Smith');
    await page.getByTestId('input-dateOfBirth').fill('1950-05-15');
    await page.getByTestId('next-button').click();
    await page.waitForTimeout(500);
    await page.getByTestId('next-button').click();
    await page.waitForTimeout(500);

    // Should be on Care Preferences
    await expect(page.locator('[data-testid="care-preferences-step"]')).toBeVisible();

    // Select care level using Picker
    await page.getByTestId('input-careLevel').click();
    await page.getByRole('option', { name: 'Assisted' }).click();

    // Check dietary requirements
    await page.getByTestId('checkbox-LowSodium').check();
    await page.getByTestId('checkbox-requiresNightChecks').check();

    await page.screenshot({ path: 'test-results/screenshots/wizard-step3-filled.png' });

    await page.getByTestId('next-button').click();
    await page.waitForTimeout(500);

    await expect(page.getByRole('heading', { level: 2 })).toContainText('Emergency Contacts');
  });

  test('Step 4: Emergency Contacts requires at least one contact', async ({ page }) => {
    // Navigate to step 4
    await page.getByTestId('input-firstName').fill('John');
    await page.getByTestId('input-lastName').fill('Smith');
    await page.getByTestId('input-dateOfBirth').fill('1950-05-15');
    await page.getByTestId('next-button').click();
    await page.waitForTimeout(500);
    await page.getByTestId('next-button').click();
    await page.waitForTimeout(500);
    await page.getByTestId('next-button').click();
    await page.waitForTimeout(500);

    // Should be on Emergency Contacts step
    await expect(page.locator('[data-testid="emergency-contacts-step"]')).toBeVisible();

    await page.screenshot({ path: 'test-results/screenshots/wizard-step4-empty.png' });

    // Add a contact
    await page.getByRole('button', { name: '+ Add Contact' }).click();

    // Fill contact details
    await page.getByTestId('input-primaryContactName').fill('Jane Smith');
    await page.getByTestId('input-primaryContactRelationship').fill('Daughter');
    await page.getByTestId('input-primaryContactPhone').fill('555-123-4567');

    await page.screenshot({ path: 'test-results/screenshots/wizard-step4-filled.png' });
  });

  test('Step 5: Review shows entered data with consent checkboxes', async ({ page }) => {
    // Navigate through all steps to review
    await page.getByTestId('input-firstName').fill('John');
    await page.getByTestId('input-lastName').fill('Smith');
    await page.getByTestId('input-dateOfBirth').fill('1950-05-15');
    await page.getByTestId('next-button').click();
    await page.waitForTimeout(500);

    // Step 2
    await page.getByTestId('next-button').click();
    await page.waitForTimeout(500);

    // Step 3
    await page.getByTestId('next-button').click();
    await page.waitForTimeout(500);

    // Step 4
    await page.getByRole('button', { name: '+ Add Contact' }).click();
    await page.getByTestId('input-primaryContactName').fill('Jane Smith');
    await page.getByTestId('input-primaryContactRelationship').fill('Daughter');
    await page.getByTestId('input-primaryContactPhone').fill('555-123-4567');
    await page.getByTestId('next-button').click();
    await page.waitForTimeout(500);

    // Should be on Review step
    await expect(page.locator('[data-testid="review-step"]')).toBeVisible();

    await page.screenshot({ path: 'test-results/screenshots/wizard-step5-review.png' });

    // Verify review shows entered data
    await expect(page.locator('.review-step')).toContainText('John');
    await expect(page.locator('.review-step')).toContainText('Smith');

    // Accept consent
    await page.getByTestId('checkbox-consent').check();
    await page.getByTestId('checkbox-terms').check();

    await page.screenshot({ path: 'test-results/screenshots/wizard-step5-consent.png' });
  });

  test('Back button navigates to previous step preserving data', async ({ page }) => {
    // Fill step 1 and go to step 2
    await page.getByTestId('input-firstName').fill('John');
    await page.getByTestId('input-lastName').fill('Smith');
    await page.getByTestId('input-dateOfBirth').fill('1950-05-15');
    await page.getByTestId('next-button').click();
    await page.waitForTimeout(500);

    // Verify on step 2
    await expect(page.locator('[data-testid="medical-history-step"]')).toBeVisible();

    // Click Back
    await page.getByTestId('back-button').click();
    await page.waitForTimeout(500);

    // Verify back on step 1
    await expect(page.locator('[data-testid="basic-info-step"]')).toBeVisible();

    // Verify data is preserved
    await expect(page.getByTestId('input-firstName')).toHaveValue('John');

    await page.screenshot({ path: 'test-results/screenshots/wizard-back-navigation.png' });
  });
});
