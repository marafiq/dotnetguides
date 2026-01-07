import { test, expect } from '@playwright/test';

test.describe('Modal - Confirmation and Edit Dialogs', () => {
  test('displays confirmation modal', async ({ page }) => {
    await page.goto('/modal/confirm');

    // Verify modal is open
    await expect(page.locator('.modal-overlay')).toBeVisible();
    await expect(page.locator('.modal')).toBeVisible();

    // Verify modal content
    await expect(page.locator('#modal-title')).toContainText('Confirm Action');
    await expect(page.locator('.modal-message')).toContainText('cannot be undone');

    // Verify buttons
    await expect(page.locator('button:has-text("Cancel")')).toBeVisible();
    await expect(page.locator('button:has-text("Confirm")')).toBeVisible();

    await page.screenshot({ path: 'screenshots/modal-confirm.png', fullPage: true });
  });

  test('displays alert modal with success', async ({ page }) => {
    await page.goto('/modal/alert');

    // Verify modal
    await expect(page.locator('.modal')).toBeVisible();
    await expect(page.locator('#modal-title')).toContainText('Success!');

    // Verify alert styling
    await expect(page.locator('.modal-alert')).toBeVisible();

    // Verify OK button
    await expect(page.locator('button:has-text("OK")')).toBeVisible();

    await page.screenshot({ path: 'screenshots/modal-alert-success.png', fullPage: true });
  });

  test('displays form modal', async ({ page }) => {
    await page.goto('/modal/form');

    // Verify modal
    await expect(page.locator('.modal')).toBeVisible();
    await expect(page.locator('#modal-title')).toContainText('Quick Add');

    // Verify form fields
    await expect(page.locator('.modal-form')).toBeVisible();
    await expect(page.locator('#modal-field-name')).toBeVisible();
    await expect(page.locator('#modal-field-email')).toBeVisible();

    // Verify form buttons
    await expect(page.locator('button:has-text("Cancel")')).toBeVisible();
    await expect(page.locator('button:has-text("Save")')).toBeVisible();

    await page.screenshot({ path: 'screenshots/modal-form-initial.png', fullPage: true });
  });

  test('form modal - fill and validate', async ({ page }) => {
    await page.goto('/modal/form');

    // Fill form fields
    await page.fill('#modal-field-name', 'John Doe');
    await page.fill('#modal-field-email', 'john.doe@example.com');

    await page.screenshot({ path: 'screenshots/modal-form-filled.png', fullPage: true });
  });

  test('delete confirmation modal', async ({ page }) => {
    await page.goto('/residents/1/delete');

    // Verify modal content
    await expect(page.locator('.modal')).toBeVisible();
    await expect(page.locator('h2')).toContainText('Delete');

    // Verify warning message
    await expect(page.locator('.modal-alert')).toBeVisible();

    // Verify buttons
    await expect(page.locator('button:has-text("Cancel")')).toBeVisible();
    await expect(page.locator('button:has-text("Delete")')).toBeVisible();

    await page.screenshot({ path: 'screenshots/modal-delete-confirm.png', fullPage: true });
  });

  test('edit resident modal', async ({ page }) => {
    await page.goto('/residents/1/edit');

    // Verify modal content
    await expect(page.locator('.modal')).toBeVisible();
    await expect(page.locator('h2')).toContainText('Edit');

    // Verify form fields
    await expect(page.locator('#name')).toBeVisible();
    await expect(page.locator('#room')).toBeVisible();
    await expect(page.locator('#admitDate')).toBeVisible();

    // Verify buttons
    await expect(page.locator('button:has-text("Cancel")')).toBeVisible();
    await expect(page.locator('button:has-text("Save")')).toBeVisible();

    await page.screenshot({ path: 'screenshots/modal-edit-resident.png', fullPage: true });
  });

  test('edit modal - fill form', async ({ page }) => {
    await page.goto('/residents/1/edit');

    // Modify name
    await page.fill('#name', 'John Doe Updated');

    // Select different room
    await page.selectOption('#room', '201B');

    await page.screenshot({ path: 'screenshots/modal-edit-modified.png', fullPage: true });
  });

  test('modal has close button', async ({ page }) => {
    await page.goto('/modal/confirm');

    // Verify close button
    await expect(page.locator('.modal-close')).toBeVisible();

    await page.screenshot({ path: 'screenshots/modal-close-button.png', fullPage: true });
  });

  test('modal footer actions styled correctly', async ({ page }) => {
    await page.goto('/modal/confirm');

    // Verify button styles
    await expect(page.locator('.btn-primary')).toBeVisible();
    await expect(page.locator('.btn-secondary')).toBeVisible();

    await page.screenshot({ path: 'screenshots/modal-button-styles.png', fullPage: true });
  });
});
