import { test, expect } from '../fixtures';

test.describe('Admission Wizard Feature', () => {
  test.beforeEach(async ({ page }) => {
    await page.goto('/');
    await page.getByTestId('nav-admission').click();
    await expect(page.getByTestId('admission-wizard')).toBeVisible();
  });

  test('Step 1: Basic Info - shows validation errors for empty fields', async ({ page }) => {
    await page.getByTestId('next-button').click();
    await expect(page.getByTestId('error-firstName')).toBeVisible();
    await expect(page.getByTestId('error-lastName')).toBeVisible();
    await page.screenshot({ path: 'test-results/screenshots/step1-validation-errors.png' });
  });

  test('Step 1: Basic Info - can fill and proceed', async ({ page }) => {
    await page.getByTestId('input-firstName').fill('John');
    await page.getByTestId('input-lastName').fill('Smith');
    await page.getByTestId('input-dateOfBirth').fill('1950-01-15');
    await page.screenshot({ path: 'test-results/screenshots/step1-filled.png' });
    await page.getByTestId('next-button').click();
    await expect(page.getByTestId('medical-history-step')).toBeVisible();
    await page.screenshot({ path: 'test-results/screenshots/step2-medical-history.png' });
  });

  test('Step 2: Medical History - optional fields', async ({ page }) => {
    await page.getByTestId('input-firstName').fill('John');
    await page.getByTestId('input-lastName').fill('Smith');
    await page.getByTestId('input-dateOfBirth').fill('1950-01-15');
    await page.getByTestId('next-button').click();
    await expect(page.getByTestId('medical-history-step')).toBeVisible();
    await page.getByTestId('input-conditions').fill('Diabetes\nHypertension');
    await page.getByTestId('input-medications').fill('Metformin\nLisinopril');
    await page.screenshot({ path: 'test-results/screenshots/step2-filled.png' });
    await page.getByTestId('next-button').click();
    await expect(page.getByTestId('care-preferences-step')).toBeVisible();
  });

  test('Step 3: Care Preferences - select care level and dietary', async ({ page }) => {
    await page.getByTestId('input-firstName').fill('John');
    await page.getByTestId('input-lastName').fill('Smith');
    await page.getByTestId('input-dateOfBirth').fill('1950-01-15');
    await page.getByTestId('next-button').click();
    await page.getByTestId('next-button').click();
    await expect(page.getByTestId('care-preferences-step')).toBeVisible();
    await page.getByTestId('input-careLevel').selectOption('Assisted');
    await page.getByTestId('checkbox-LowSodium').check();
    await page.getByTestId('checkbox-requiresNightChecks').check();
    await page.screenshot({ path: 'test-results/screenshots/step3-care-preferences.png' });
    await page.getByTestId('next-button').click();
    await expect(page.getByTestId('emergency-contacts-step')).toBeVisible();
  });

  test('Step 4: Emergency Contacts - validates required fields', async ({ page }) => {
    await page.getByTestId('input-firstName').fill('John');
    await page.getByTestId('input-lastName').fill('Smith');
    await page.getByTestId('input-dateOfBirth').fill('1950-01-15');
    await page.getByTestId('next-button').click();
    await page.getByTestId('next-button').click();
    await page.getByTestId('next-button').click();
    await expect(page.getByTestId('emergency-contacts-step')).toBeVisible();
    await page.getByTestId('next-button').click();
    await expect(page.getByTestId('error-primaryContactName')).toBeVisible();
    await page.screenshot({ path: 'test-results/screenshots/step4-validation-errors.png' });
  });

  test('Step 5: Review - shows all entered data', async ({ page }) => {
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
    await expect(page.getByTestId('review-step')).toBeVisible();
    await expect(page.getByText('John Smith')).toBeVisible();
    await page.screenshot({ path: 'test-results/screenshots/step5-review.png' });
  });

  test('Step 5: Review - requires consent checkboxes', async ({ page }) => {
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
    await page.getByTestId('next-button').click();
    await expect(page.getByTestId('error-consent')).toBeVisible();
    await page.screenshot({ path: 'test-results/screenshots/step5-consent-errors.png' });
  });

  test('Complete admission flow successfully', async ({ page }) => {
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
    await page.getByTestId('checkbox-consent').check();
    await page.getByTestId('checkbox-terms').check();
    await page.screenshot({ path: 'test-results/screenshots/step5-consent-given.png' });
    await page.getByTestId('next-button').click();
    await expect(page.getByTestId('residents-list')).toBeVisible();
    await page.screenshot({ path: 'test-results/screenshots/admission-complete.png' });
  });

  test('Back button navigates to previous step', async ({ page }) => {
    await page.getByTestId('input-firstName').fill('John');
    await page.getByTestId('input-lastName').fill('Smith');
    await page.getByTestId('input-dateOfBirth').fill('1950-01-15');
    await page.getByTestId('next-button').click();
    await expect(page.getByTestId('medical-history-step')).toBeVisible();
    await page.getByTestId('back-button').click();
    await expect(page.getByTestId('basic-info-step')).toBeVisible();
    await expect(page.getByTestId('input-firstName')).toHaveValue('John');
    await page.screenshot({ path: 'test-results/screenshots/back-button-data-preserved.png' });
  });
});
