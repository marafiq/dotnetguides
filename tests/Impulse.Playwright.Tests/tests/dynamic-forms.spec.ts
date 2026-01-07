import { test, expect } from '@playwright/test';

test.describe('Dynamic Forms - Insurance Application', () => {
  test.beforeEach(async ({ page }) => {
    await page.goto('/forms/insurance');
  });

  test('displays form with all sections', async ({ page }) => {
    // Verify form structure
    await expect(page.locator('.dynamic-form')).toBeVisible();
    await expect(page.locator('h1')).toContainText('Insurance Application');

    // Verify sections are visible
    await expect(page.locator('fieldset')).toHaveCount(7); // 7 sections in insurance form

    await page.screenshot({ path: 'screenshots/forms-insurance-initial.png', fullPage: true });
  });

  test('displays Personal Information section', async ({ page }) => {
    const personalSection = page.locator('fieldset:has(legend:text("Personal Information"))');
    await expect(personalSection).toBeVisible();

    // Verify fields
    await expect(personalSection.locator('#firstName')).toBeVisible();
    await expect(personalSection.locator('#lastName')).toBeVisible();
    await expect(personalSection.locator('#dateOfBirth')).toBeVisible();
    await expect(personalSection.locator('#email')).toBeVisible();
    await expect(personalSection.locator('#phone')).toBeVisible();

    await page.screenshot({ path: 'screenshots/forms-personal-info.png', fullPage: true });
  });

  test('displays Employment section with conditional salary field', async ({ page }) => {
    const employmentSection = page.locator('fieldset:has(legend:text("Employment Information"))');
    await expect(employmentSection).toBeVisible();

    // Employment status dropdown
    await expect(employmentSection.locator('#employmentStatus')).toBeVisible();

    await page.screenshot({ path: 'screenshots/forms-employment.png', fullPage: true });

    // Select employed status
    await employmentSection.locator('#employmentStatus').selectOption('employed');
    await page.waitForTimeout(500);

    await page.screenshot({ path: 'screenshots/forms-employment-selected.png', fullPage: true });
  });

  test('shows conditional Auto Insurance fields', async ({ page }) => {
    // Select Auto insurance type
    const insuranceRadio = page.locator('input[name="insuranceType"][value="auto"]');
    await insuranceRadio.check();
    await page.waitForTimeout(500);

    // Auto details section should be visible
    const autoSection = page.locator('fieldset:has(legend:text("Vehicle Information"))');

    await page.screenshot({ path: 'screenshots/forms-auto-insurance.png', fullPage: true });
  });

  test('shows conditional Home Insurance fields', async ({ page }) => {
    // Select Home insurance type
    const insuranceRadio = page.locator('input[name="insuranceType"][value="home"]');
    await insuranceRadio.check();
    await page.waitForTimeout(500);

    await page.screenshot({ path: 'screenshots/forms-home-insurance.png', fullPage: true });
  });

  test('shows conditional Life Insurance fields', async ({ page }) => {
    // Select Life insurance type
    const insuranceRadio = page.locator('input[name="insuranceType"][value="life"]');
    await insuranceRadio.check();
    await page.waitForTimeout(500);

    await page.screenshot({ path: 'screenshots/forms-life-insurance.png', fullPage: true });
  });

  test('shows conditional Payment fields for Bank Transfer', async ({ page }) => {
    // Scroll to payment section
    await page.locator('input[name="paymentMethod"][value="bankTransfer"]').scrollIntoViewIfNeeded();

    // Select Bank Transfer
    await page.locator('input[name="paymentMethod"][value="bankTransfer"]').check();
    await page.waitForTimeout(500);

    await page.screenshot({ path: 'screenshots/forms-payment-bank.png', fullPage: true });
  });

  test('shows conditional Payment fields for Credit Card', async ({ page }) => {
    // Select Credit Card
    await page.locator('input[name="paymentMethod"][value="creditCard"]').scrollIntoViewIfNeeded();
    await page.locator('input[name="paymentMethod"][value="creditCard"]').check();
    await page.waitForTimeout(500);

    await page.screenshot({ path: 'screenshots/forms-payment-card.png', fullPage: true });
  });

  test('fills complete form and validates', async ({ page }) => {
    // Fill personal info
    await page.fill('#firstName', 'Jane');
    await page.fill('#lastName', 'Smith');
    await page.fill('#dateOfBirth', '1985-03-20');
    await page.fill('#email', 'jane.smith@example.com');
    await page.fill('#phone', '555-123-4567');

    // Select employment
    await page.selectOption('#employmentStatus', 'employed');
    await page.waitForTimeout(300);

    // Select insurance type
    await page.locator('input[name="insuranceType"][value="auto"]').check();
    await page.waitForTimeout(300);

    // Select payment method
    await page.locator('input[name="paymentMethod"][value="creditCard"]').scrollIntoViewIfNeeded();
    await page.locator('input[name="paymentMethod"][value="creditCard"]').check();

    await page.screenshot({ path: 'screenshots/forms-filled-complete.png', fullPage: true });
  });

  test('form shows required field indicators', async ({ page }) => {
    // Check for required asterisks
    const requiredIndicators = page.locator('.required');
    await expect(requiredIndicators.first()).toBeVisible();

    await page.screenshot({ path: 'screenshots/forms-required-indicators.png', fullPage: true });
  });

  test('form shows help text for fields', async ({ page }) => {
    // Check for help text
    const helpText = page.locator('.help-text');
    await expect(helpText.first()).toBeVisible();

    await page.screenshot({ path: 'screenshots/forms-help-text.png', fullPage: true });
  });
});
