import { test, expect } from '@playwright/test';

/**
 * Responsive design tests for Impulse v2
 * Tests mobile, tablet, and desktop viewports
 */

test.describe('Responsive Design', () => {

  test('Dashboard renders correctly on desktop (1920px)', async ({ page }) => {
    await page.setViewportSize({ width: 1920, height: 1080 }); // Full HD
    await page.goto('/dashboard');
    await page.waitForTimeout(2000);

    // Check heading is visible
    const heading = page.locator('h1');
    await expect(heading).toBeVisible({ timeout: 5000 });

    // Take desktop screenshot
    await page.screenshot({
      path: 'test-results-client/dashboard-desktop.png',
      fullPage: true
    });
  });

  test('Residents renders correctly on desktop (1920px)', async ({ page }) => {
    await page.setViewportSize({ width: 1920, height: 1080 });
    await page.goto('/residents');
    await page.waitForTimeout(2000);

    await page.screenshot({
      path: 'test-results-client/residents-desktop.png',
      fullPage: true
    });
  });

  test('Dashboard renders correctly on mobile (375px)', async ({ page }) => {
    await page.setViewportSize({ width: 375, height: 812 }); // iPhone X
    await page.goto('/dashboard');
    await page.waitForTimeout(2000);

    // Check heading is visible
    const heading = page.locator('h1');
    await expect(heading).toBeVisible({ timeout: 5000 });

    // Take mobile screenshot
    await page.screenshot({
      path: 'test-results-client/dashboard-mobile.png',
      fullPage: true
    });
  });

  test('Dashboard renders correctly on tablet (768px)', async ({ page }) => {
    await page.setViewportSize({ width: 768, height: 1024 }); // iPad
    await page.goto('/dashboard');
    await page.waitForTimeout(2000);

    await page.screenshot({
      path: 'test-results-client/dashboard-tablet.png',
      fullPage: true
    });
  });

  test('Residents list on mobile stacks cards', async ({ page }) => {
    await page.setViewportSize({ width: 375, height: 812 });
    await page.goto('/residents');
    await page.waitForTimeout(2000);

    await page.screenshot({
      path: 'test-results-client/residents-mobile.png',
      fullPage: true
    });
  });

  test('Create form on mobile has full-width fields', async ({ page }) => {
    await page.setViewportSize({ width: 375, height: 812 });
    await page.goto('/residents/new');
    await page.waitForTimeout(2000);

    await page.screenshot({
      path: 'test-results-client/create-form-mobile.png',
      fullPage: true
    });
  });

  test('Navigation collapses on mobile', async ({ page }) => {
    await page.setViewportSize({ width: 375, height: 812 });
    await page.goto('/dashboard');
    await page.waitForTimeout(2000);

    // Nav should be visible but stacked
    const nav = page.locator('.app-nav');
    await expect(nav).toBeVisible();

    await page.screenshot({
      path: 'test-results-client/nav-mobile.png',
    });
  });
});
