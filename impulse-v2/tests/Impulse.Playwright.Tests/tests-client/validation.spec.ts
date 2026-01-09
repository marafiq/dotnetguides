import { test, expect } from '@playwright/test';

/**
 * Validation tests for Impulse v2
 * Demonstrates Zod validation with nested objects (Impulse way)
 */

test.describe('Impulse Validation System', () => {

  test('Shows validation errors for empty required fields', async ({ page }) => {
    await page.setViewportSize({ width: 1920, height: 1080 });
    await page.goto('/residents/new');
    await page.waitForTimeout(1000);

    // Click Create without filling anything
    await page.click('button[type="submit"]');
    await page.waitForTimeout(500);

    // Take screenshot of validation errors
    await page.screenshot({
      path: 'test-results-client/validation-required-fields.png',
      fullPage: true
    });
  });

  test('Shows nested validation for Address fields', async ({ page }) => {
    await page.setViewportSize({ width: 1920, height: 1080 });
    await page.goto('/residents/new');
    await page.waitForTimeout(1000);

    // Fill basic required fields
    await page.fill('input[name="firstName"], input:below(:text("First Name"))', 'John');
    await page.fill('input[name="lastName"], input:below(:text("Last Name"))', 'Doe');
    await page.fill('input[name="dateOfBirth"], input:below(:text("Date of Birth"))', '1945-03-15');
    await page.fill('input[name="roomNumber"], input:below(:text("Room Number"))', '101A');

    // Enable Address section
    const addressCheckbox = page.locator('text=Include address').locator('..');
    await addressCheckbox.click();
    await page.waitForTimeout(500);

    // Try to submit with empty address fields (partial nested object)
    await page.click('button[type="submit"]');
    await page.waitForTimeout(500);

    // Take screenshot showing nested address validation
    await page.screenshot({
      path: 'test-results-client/validation-nested-address.png',
      fullPage: true
    });
  });

  test('Shows nested validation for Emergency Contacts', async ({ page }) => {
    await page.setViewportSize({ width: 1920, height: 1080 });
    await page.goto('/residents/new');
    await page.waitForTimeout(1000);

    // Fill basic required fields
    await page.fill('input[name="firstName"], input:below(:text("First Name"))', 'Jane');
    await page.fill('input[name="lastName"], input:below(:text("Last Name"))', 'Smith');
    await page.fill('input[name="dateOfBirth"], input:below(:text("Date of Birth"))', '1950-07-22');
    await page.fill('input[name="roomNumber"], input:below(:text("Room Number"))', '102B');

    // Add an emergency contact
    await page.click('text=+ Add Contact');
    await page.waitForTimeout(500);

    // Fill partial emergency contact (leave some fields empty)
    const contactCard = page.locator('.impulse-contact-card, [class*="contact"]').first();

    // Try to submit with incomplete emergency contact
    await page.click('button[type="submit"]');
    await page.waitForTimeout(500);

    // Take screenshot showing nested emergency contact validation
    await page.screenshot({
      path: 'test-results-client/validation-nested-contacts.png',
      fullPage: true
    });
  });

  test('Shows date format validation (Zod datetime)', async ({ page }) => {
    await page.setViewportSize({ width: 1920, height: 1080 });
    await page.goto('/residents/new');
    await page.waitForTimeout(1000);

    // Fill fields with invalid date format
    await page.fill('input[name="firstName"], input:below(:text("First Name"))', 'Bob');
    await page.fill('input[name="lastName"], input:below(:text("Last Name"))', 'Jones');
    await page.fill('input[name="dateOfBirth"], input:below(:text("Date of Birth"))', 'invalid-date');
    await page.fill('input[name="roomNumber"], input:below(:text("Room Number"))', '103C');

    // Submit to trigger date validation
    await page.click('button[type="submit"]');
    await page.waitForTimeout(500);

    // Take screenshot showing date validation error
    await page.screenshot({
      path: 'test-results-client/validation-date-format.png',
      fullPage: true
    });
  });

  test('Shows Zod validation errors with nested paths', async ({ page }) => {
    await page.setViewportSize({ width: 1920, height: 1080 });
    await page.goto('/residents/new');
    await page.waitForTimeout(1000);

    // Submit empty form to trigger Zod validation
    await page.click('button[type="submit"]');
    await page.waitForTimeout(1000);

    // Take screenshot showing Zod validation errors
    await page.screenshot({
      path: 'test-results-client/validation-zod-errors.png',
      fullPage: true
    });

    // Now fill some fields but leave date invalid
    const firstNameInput = page.locator('input').first();
    await firstNameInput.fill('Test');

    // Submit again to see remaining errors
    await page.click('button[type="submit"]');
    await page.waitForTimeout(500);

    await page.screenshot({
      path: 'test-results-client/validation-partial-errors.png',
      fullPage: true
    });
  });
});
