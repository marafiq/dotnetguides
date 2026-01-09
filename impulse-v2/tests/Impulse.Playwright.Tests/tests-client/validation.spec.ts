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

    // Fill basic required fields with VALID data including proper datetime
    const inputs = page.locator('input[type="text"]');
    await inputs.nth(0).fill('John');  // First Name
    await inputs.nth(1).fill('Doe');   // Last Name
    await inputs.nth(2).fill('1945-03-15T00:00:00Z');  // Date of Birth - valid ISO datetime
    await inputs.nth(3).fill('101A');  // Room Number

    // Enable Address section by clicking the checkbox
    await page.click('text=Include address');
    await page.waitForTimeout(500);

    // Touch Street field to initialize address object, leave City/State/ZIP empty
    // This triggers Zod nested validation for address.city, address.state, address.zipCode
    const streetInput = page.locator('input[type="text"]').nth(4);
    await streetInput.fill('123 Main St');  // Fill street only

    // Try to submit - should show nested address validation errors for city, state, zipCode
    await page.click('button[type="submit"]');
    await page.waitForTimeout(1000);

    // Take screenshot showing nested address validation errors
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

  test('Shows client-side validation passes, demonstrating Zod + S2 integration', async ({ page }) => {
    await page.setViewportSize({ width: 1920, height: 1080 });
    await page.goto('/residents/new');
    await page.waitForTimeout(1000);

    // Fill ALL fields with valid data - this passes CLIENT-SIDE Zod validation
    const inputs = page.locator('input[type="text"]');
    await inputs.nth(0).fill('John');  // First Name - valid
    await inputs.nth(1).fill('Smith');   // Last Name - valid
    await inputs.nth(2).fill('1945-03-15T00:00:00Z');  // Date of Birth - valid ISO datetime
    await inputs.nth(3).fill('101A');  // Room Number - valid

    // Add emergency contact (required by server)
    await page.click('text=+ Add Contact');
    await page.waitForTimeout(300);

    // Fill emergency contact fields
    const contactInputs = page.locator('.impulse-contact-card input[type="text"]');
    await contactInputs.nth(0).fill('Jane Doe');  // Name
    await contactInputs.nth(1).fill('Spouse');     // Relationship
    await contactInputs.nth(2).fill('555-1234');   // Phone
    await contactInputs.nth(3).fill('jane@example.com');  // Email

    // Take screenshot before submit - showing filled valid form
    await page.screenshot({
      path: 'test-results-client/validation-client-valid-form.png',
      fullPage: true
    });

    // Submit - should pass CLIENT validation (Zod)
    // Note: In preview mode without backend, this will succeed with mock
    await page.click('button[type="submit"]');
    await page.waitForTimeout(1000);

    // Take screenshot of result
    await page.screenshot({
      path: 'test-results-client/validation-client-passed.png',
      fullPage: true
    });
  });

  test('Server-side validation with reserved names (ProblemDetails)', async ({ page }) => {
    await page.setViewportSize({ width: 1920, height: 1080 });
    await page.goto('/residents/new');
    await page.waitForTimeout(1000);

    // Fill form with data that passes CLIENT validation but fails SERVER validation
    // Using "Admin" as first name - this is a RESERVED name on the server
    const inputs = page.locator('input[type="text"]');
    await inputs.nth(0).fill('Admin');  // First Name - RESERVED on server!
    await inputs.nth(1).fill('User');   // Last Name - valid
    await inputs.nth(2).fill('1945-03-15T00:00:00Z');  // Date of Birth - valid
    await inputs.nth(3).fill('101A');  // Room Number - valid

    // Add emergency contact (required by server)
    await page.click('text=+ Add Contact');
    await page.waitForTimeout(300);

    const contactInputs = page.locator('.impulse-contact-card input[type="text"]');
    await contactInputs.nth(0).fill('Jane Doe');
    await contactInputs.nth(1).fill('Spouse');
    await contactInputs.nth(2).fill('555-1234');
    await contactInputs.nth(3).fill('jane@example.com');

    // Take screenshot showing form with reserved name filled
    await page.screenshot({
      path: 'test-results-client/validation-server-reserved-name.png',
      fullPage: true
    });

    // This form passes CLIENT-SIDE Zod validation but would fail SERVER-SIDE
    // because "Admin" is a reserved name in the .NET endpoint
    // When backend is running, this demonstrates ProblemDetails error handling

    // Submit the form
    await page.click('button[type="submit"]');
    await page.waitForTimeout(1500);

    // Take screenshot of result (mock success in preview, server error with backend)
    await page.screenshot({
      path: 'test-results-client/validation-server-response.png',
      fullPage: true
    });
  });
});
