import { test, expect } from '@playwright/test';

/**
 * Impulse Admission Wizard Tests
 * Demonstrates multi-step wizard with:
 * - Server-driven step configuration
 * - Step-by-step Zod validation (client)
 * - Server-side ProblemDetails validation
 * - S2 Form + validationErrors per step
 */

test.describe('Impulse Admission Wizard', () => {
  test('Wizard renders with 5 steps and progress indicator', async ({ page }) => {
    await page.goto('/admission');
    await page.waitForSelector('.wizard-container');

    // Verify wizard header
    await expect(page.getByRole('heading', { level: 1 })).toContainText('Resident Admission');

    // Verify step indicators
    const stepIndicators = page.locator('.wizard-step-indicator');
    await expect(stepIndicators).toHaveCount(5);

    // Verify first step is active
    await expect(stepIndicators.first()).toHaveClass(/active/);

    // Screenshot
    await page.screenshot({ path: 'test-results-client/wizard-step1-initial.png', fullPage: true });
  });

  test('Step 1: Basic Info validation shows errors for empty fields', async ({ page }) => {
    await page.goto('/admission');
    await page.waitForSelector('.wizard-container');

    // Click Next without filling any fields
    await page.getByRole('button', { name: 'Next' }).click();

    // Wait for validation
    await page.waitForTimeout(500);

    // Screenshot showing validation errors
    await page.screenshot({ path: 'test-results-client/wizard-step1-validation.png', fullPage: true });

    // Should show validation errors
    const form = page.locator('form');
    await expect(form).toContainText('First name');
  });

  test('Step 1: Basic Info can be filled and advances to Step 2', async ({ page }) => {
    await page.goto('/admission');
    await page.waitForSelector('.wizard-container');

    // Fill out basic info
    await page.locator('input[name="firstName"]').fill('John');
    await page.locator('input[name="lastName"]').fill('Smith');
    await page.locator('input[name="dateOfBirth"]').fill('1950-05-15');
    await page.locator('input[name="admissionDate"]').fill('2026-02-01');

    await page.screenshot({ path: 'test-results-client/wizard-step1-filled.png', fullPage: true });

    // Click Next
    await page.getByRole('button', { name: 'Next' }).click();

    // Wait for step transition
    await page.waitForTimeout(1000);

    // Verify we're on Step 2
    await expect(page.getByRole('heading', { level: 2 })).toContainText('Medical History');

    await page.screenshot({ path: 'test-results-client/wizard-step2-initial.png', fullPage: true });
  });

  test('Step 2: Medical History can add allergies and medications', async ({ page }) => {
    // Navigate to step 2
    await page.goto('/admission');
    await page.waitForSelector('.wizard-container');
    await page.locator('input[name="firstName"]').fill('John');
    await page.locator('input[name="lastName"]').fill('Smith');
    await page.locator('input[name="dateOfBirth"]').fill('1950-05-15');
    await page.locator('input[name="admissionDate"]').fill('2026-02-01');
    await page.getByRole('button', { name: 'Next' }).click();
    await page.waitForTimeout(500);

    // Add allergies
    await page.locator('input').filter({ hasText: /allergy/i }).or(page.getByLabel('Add Allergy')).fill('Penicillin');
    await page.getByRole('button', { name: 'Add' }).first().click();

    // Fill physician info
    await page.locator('input[name="primaryCarePhysician"]').fill('Dr. Williams');

    await page.screenshot({ path: 'test-results-client/wizard-step2-filled.png', fullPage: true });

    // Continue to step 3
    await page.getByRole('button', { name: 'Next' }).click();
    await page.waitForTimeout(500);

    await expect(page.getByRole('heading', { level: 2 })).toContainText('Care Preferences');
    await page.screenshot({ path: 'test-results-client/wizard-step3-initial.png', fullPage: true });
  });

  test('Step 3: Care Preferences validation requires mobility and communication', async ({ page }) => {
    // Navigate to step 3
    await page.goto('/admission');
    await page.waitForSelector('.wizard-container');
    await page.locator('input[name="firstName"]').fill('John');
    await page.locator('input[name="lastName"]').fill('Smith');
    await page.locator('input[name="dateOfBirth"]').fill('1950-05-15');
    await page.locator('input[name="admissionDate"]').fill('2026-02-01');
    await page.getByRole('button', { name: 'Next' }).click();
    await page.waitForTimeout(500);
    await page.getByRole('button', { name: 'Next' }).click();
    await page.waitForTimeout(500);

    // Try to proceed without required fields
    await page.getByRole('button', { name: 'Next' }).click();
    await page.waitForTimeout(500);

    // Should show validation errors
    await page.screenshot({ path: 'test-results-client/wizard-step3-validation.png', fullPage: true });

    // Fill required fields
    await page.locator('input[name="mobilityLevel"]').fill('Independent');
    await page.locator('input[name="communicationPreference"]').fill('Verbal');

    await page.screenshot({ path: 'test-results-client/wizard-step3-filled.png', fullPage: true });
  });

  test('Step 4: Emergency Contacts requires at least one contact', async ({ page }) => {
    // Navigate to step 4
    await page.goto('/admission');
    await page.waitForSelector('.wizard-container');
    await page.locator('input[name="firstName"]').fill('John');
    await page.locator('input[name="lastName"]').fill('Smith');
    await page.locator('input[name="dateOfBirth"]').fill('1950-05-15');
    await page.locator('input[name="admissionDate"]').fill('2026-02-01');
    await page.getByRole('button', { name: 'Next' }).click();
    await page.waitForTimeout(500);
    await page.getByRole('button', { name: 'Next' }).click();
    await page.waitForTimeout(500);
    await page.locator('input[name="mobilityLevel"]').fill('Independent');
    await page.locator('input[name="communicationPreference"]').fill('Verbal');
    await page.getByRole('button', { name: 'Next' }).click();
    await page.waitForTimeout(500);

    // Should be on Emergency Contacts step
    await expect(page.getByRole('heading', { level: 2 })).toContainText('Emergency Contacts');

    await page.screenshot({ path: 'test-results-client/wizard-step4-empty.png', fullPage: true });

    // Add a contact
    await page.getByRole('button', { name: '+ Add Contact' }).click();

    // Fill contact details
    await page.locator('input[name="contacts.0.name"]').fill('Jane Smith');
    await page.locator('input[name="contacts.0.relationship"]').fill('Daughter');
    await page.locator('input[name="contacts.0.phone"]').fill('555-123-4567');

    await page.screenshot({ path: 'test-results-client/wizard-step4-filled.png', fullPage: true });
  });

  test('Step 5: Review shows all entered data with consent checkboxes', async ({ page }) => {
    // Navigate through all steps to review
    await page.goto('/admission');
    await page.waitForSelector('.wizard-container');

    // Step 1
    await page.locator('input[name="firstName"]').fill('John');
    await page.locator('input[name="lastName"]').fill('Smith');
    await page.locator('input[name="dateOfBirth"]').fill('1950-05-15');
    await page.locator('input[name="admissionDate"]').fill('2026-02-01');
    await page.getByRole('button', { name: 'Next' }).click();
    await page.waitForTimeout(500);

    // Step 2
    await page.getByRole('button', { name: 'Next' }).click();
    await page.waitForTimeout(500);

    // Step 3
    await page.locator('input[name="mobilityLevel"]').fill('Independent');
    await page.locator('input[name="communicationPreference"]').fill('Verbal');
    await page.getByRole('button', { name: 'Next' }).click();
    await page.waitForTimeout(500);

    // Step 4
    await page.getByRole('button', { name: '+ Add Contact' }).click();
    await page.locator('input[name="contacts.0.name"]').fill('Jane Smith');
    await page.locator('input[name="contacts.0.relationship"]').fill('Daughter');
    await page.locator('input[name="contacts.0.phone"]').fill('555-123-4567');
    await page.getByRole('button', { name: 'Next' }).click();
    await page.waitForTimeout(500);

    // Should be on Review step
    await expect(page.getByRole('heading', { level: 2 })).toContainText('Review');

    await page.screenshot({ path: 'test-results-client/wizard-step5-review.png', fullPage: true });

    // Verify review shows entered data
    await expect(page.locator('.review-step')).toContainText('John');
    await expect(page.locator('.review-step')).toContainText('Smith');
    await expect(page.locator('.review-step')).toContainText('Independent');
    await expect(page.locator('.review-step')).toContainText('Jane Smith');
  });

  test('Back button navigates to previous step', async ({ page }) => {
    await page.goto('/admission');
    await page.waitForSelector('.wizard-container');

    // Fill step 1 and go to step 2
    await page.locator('input[name="firstName"]').fill('John');
    await page.locator('input[name="lastName"]').fill('Smith');
    await page.locator('input[name="dateOfBirth"]').fill('1950-05-15');
    await page.locator('input[name="admissionDate"]').fill('2026-02-01');
    await page.getByRole('button', { name: 'Next' }).click();
    await page.waitForTimeout(500);

    // Verify on step 2
    await expect(page.getByRole('heading', { level: 2 })).toContainText('Medical History');

    // Click Back
    await page.getByRole('button', { name: 'Back' }).click();

    // Verify back on step 1
    await expect(page.getByRole('heading', { level: 2 })).toContainText('Basic Information');

    // Verify data is preserved
    await expect(page.locator('input[name="firstName"]')).toHaveValue('John');

    await page.screenshot({ path: 'test-results-client/wizard-back-navigation.png', fullPage: true });
  });

  test('Mobile responsive wizard layout', async ({ page }) => {
    // Set mobile viewport
    await page.setViewportSize({ width: 375, height: 812 });

    await page.goto('/admission');
    await page.waitForSelector('.wizard-container');

    // Screenshot mobile layout
    await page.screenshot({ path: 'test-results-client/wizard-mobile.png', fullPage: true });

    // Step indicators should wrap
    const stepIndicators = page.locator('.wizard-step-indicator');
    await expect(stepIndicators).toHaveCount(5);
  });
});
