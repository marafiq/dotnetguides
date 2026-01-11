import { test, expect } from '@playwright/test';

test.describe('Admission Wizard Feature', () => {
  test.beforeEach(async ({ page }) => {
    await page.goto('/');
    // Navigate to admission wizard
    await page.getByTestId('nav-admission').click();
    await expect(page.getByTestId('admission-wizard')).toBeVisible();
  });

  test('Step 1: Basic Info - shows validation errors for empty fields', async ({ page }) => {
    // Try to proceed without filling required fields
    await page.getByTestId('next-button').click();

    // Should show validation errors
    await expect(page.getByTestId('error-firstName')).toBeVisible();
    await expect(page.getByTestId('error-lastName')).toBeVisible();
  });

  test('Step 1: Basic Info - can fill and proceed', async ({ page }) => {
    // Fill basic info
    await page.getByTestId('input-firstName').fill('John');
    await page.getByTestId('input-lastName').fill('Smith');
    await page.getByTestId('input-dateOfBirth').fill('1950-01-15');

    // Click next
    await page.getByTestId('next-button').click();

    // Should be on step 2
    await expect(page.getByTestId('medical-history-step')).toBeVisible();
  });

  test('Step 2: Medical History - optional fields', async ({ page }) => {
    // Complete step 1
    await page.getByTestId('input-firstName').fill('John');
    await page.getByTestId('input-lastName').fill('Smith');
    await page.getByTestId('input-dateOfBirth').fill('1950-01-15');
    await page.getByTestId('next-button').click();

    // Step 2 - medical history is optional
    await expect(page.getByTestId('medical-history-step')).toBeVisible();
    await page.getByTestId('input-conditions').fill('Diabetes\nHypertension');
    await page.getByTestId('input-medications').fill('Metformin\nLisinopril');

    await page.getByTestId('next-button').click();

    // Should be on step 3
    await expect(page.getByTestId('care-preferences-step')).toBeVisible();
  });

  test('Step 3: Care Preferences - select care level and dietary', async ({ page }) => {
    // Complete steps 1-2
    await page.getByTestId('input-firstName').fill('John');
    await page.getByTestId('input-lastName').fill('Smith');
    await page.getByTestId('input-dateOfBirth').fill('1950-01-15');
    await page.getByTestId('next-button').click();
    await page.getByTestId('next-button').click();

    // Step 3 - care preferences
    await expect(page.getByTestId('care-preferences-step')).toBeVisible();
    await page.getByTestId('input-careLevel').selectOption('Assisted');
    await page.getByTestId('checkbox-LowSodium').check();
    await page.getByTestId('checkbox-requiresNightChecks').check();

    await page.getByTestId('next-button').click();

    // Should be on step 4
    await expect(page.getByTestId('emergency-contacts-step')).toBeVisible();
  });

  test('Step 4: Emergency Contacts - validates required fields', async ({ page }) => {
    // Complete steps 1-3
    await page.getByTestId('input-firstName').fill('John');
    await page.getByTestId('input-lastName').fill('Smith');
    await page.getByTestId('input-dateOfBirth').fill('1950-01-15');
    await page.getByTestId('next-button').click();
    await page.getByTestId('next-button').click();
    await page.getByTestId('next-button').click();

    // Step 4 - try to proceed without required fields
    await page.getByTestId('next-button').click();

    // Should show validation errors
    await expect(page.getByTestId('error-primaryContactName')).toBeVisible();
    await expect(page.getByTestId('error-primaryContactPhone')).toBeVisible();
  });

  test('Step 5: Review - shows all entered data', async ({ page }) => {
    // Complete all steps
    await page.getByTestId('input-firstName').fill('John');
    await page.getByTestId('input-lastName').fill('Smith');
    await page.getByTestId('input-dateOfBirth').fill('1950-01-15');
    await page.getByTestId('next-button').click();

    await page.getByTestId('next-button').click();
    await page.getByTestId('next-button').click();

    await page.getByTestId('input-primaryContactName').fill('Jane Smith');
    await page.getByTestId('input-primaryContactPhone').fill('555-0123');
    await page.getByTestId('input-primaryContactRelationship').fill('Daughter');
    await page.getByTestId('next-button').click();

    // Step 5 - review
    await expect(page.getByTestId('review-step')).toBeVisible();
    await expect(page.getByText('John Smith')).toBeVisible();
    await expect(page.getByText('Jane Smith')).toBeVisible();
  });

  test('Step 5: Review - requires consent checkboxes', async ({ page }) => {
    // Complete all steps quickly
    await page.getByTestId('input-firstName').fill('John');
    await page.getByTestId('input-lastName').fill('Smith');
    await page.getByTestId('input-dateOfBirth').fill('1950-01-15');
    await page.getByTestId('next-button').click();
    await page.getByTestId('next-button').click();
    await page.getByTestId('next-button').click();
    await page.getByTestId('input-primaryContactName').fill('Jane Smith');
    await page.getByTestId('input-primaryContactPhone').fill('555-0123');
    await page.getByTestId('input-primaryContactRelationship').fill('Daughter');
    await page.getByTestId('next-button').click();

    // Try to complete without consent
    await page.getByTestId('next-button').click();

    // Should show consent errors
    await expect(page.getByTestId('error-consentGiven')).toBeVisible();
    await expect(page.getByTestId('error-termsAccepted')).toBeVisible();
  });

  test('Complete admission flow successfully', async ({ page }) => {
    // Complete all steps
    await page.getByTestId('input-firstName').fill('John');
    await page.getByTestId('input-lastName').fill('Smith');
    await page.getByTestId('input-dateOfBirth').fill('1950-01-15');
    await page.getByTestId('next-button').click();

    await page.getByTestId('next-button').click();
    await page.getByTestId('next-button').click();

    await page.getByTestId('input-primaryContactName').fill('Jane Smith');
    await page.getByTestId('input-primaryContactPhone').fill('555-0123');
    await page.getByTestId('input-primaryContactRelationship').fill('Daughter');
    await page.getByTestId('next-button').click();

    // Give consent and accept terms
    await page.getByTestId('checkbox-consent').check();
    await page.getByTestId('checkbox-terms').check();

    // Complete admission
    await page.getByTestId('next-button').click();

    // Should redirect to residents list
    await expect(page.getByTestId('residents-list')).toBeVisible();
  });

  test('Back button navigates to previous step', async ({ page }) => {
    // Complete step 1
    await page.getByTestId('input-firstName').fill('John');
    await page.getByTestId('input-lastName').fill('Smith');
    await page.getByTestId('input-dateOfBirth').fill('1950-01-15');
    await page.getByTestId('next-button').click();

    // Now on step 2
    await expect(page.getByTestId('medical-history-step')).toBeVisible();

    // Go back
    await page.getByTestId('back-button').click();

    // Should be back on step 1 with data preserved
    await expect(page.getByTestId('basic-info-step')).toBeVisible();
    await expect(page.getByTestId('input-firstName')).toHaveValue('John');
  });
});
