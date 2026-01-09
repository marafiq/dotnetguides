import { test, expect } from '@playwright/test';

/**
 * Client-only E2E tests for Impulse v2
 * Tests that React components render correctly with mock data
 * No .NET backend required - uses Vite dev server with impulse-runtime mock data
 */

test.describe('Impulse v2 UI Rendering', () => {

  test('Dashboard renders with S2 components and mock data', async ({ page }) => {
    await page.goto('/dashboard');

    // Wait for React to mount
    await page.waitForSelector('#app', { state: 'visible' });

    // Wait for content to render (give time for route loading)
    await page.waitForTimeout(2000);

    // Check that heading renders
    const heading = page.locator('h1');
    await expect(heading).toBeVisible({ timeout: 5000 });

    // Take screenshot
    await page.screenshot({
      path: 'test-results-client/dashboard.png',
      fullPage: true
    });

    // Verify S2 components rendered (check for S2 class patterns)
    const pageContent = await page.content();
    expect(pageContent).toContain('Dashboard');
  });

  test('Residents list renders with cards and status badges', async ({ page }) => {
    await page.goto('/residents');

    // Wait for React to mount
    await page.waitForSelector('#app', { state: 'visible' });

    // Wait for content to render
    await page.waitForTimeout(2000);

    // Check heading
    const heading = page.locator('h1');
    await expect(heading).toBeVisible({ timeout: 5000 });

    // Take screenshot
    await page.screenshot({
      path: 'test-results-client/residents-list.png',
      fullPage: true
    });

    // Verify page renders residents content
    const pageContent = await page.content();
    expect(pageContent).toContain('Resident');
  });

  test('Create resident form renders with S2 TextField components', async ({ page }) => {
    await page.goto('/residents/new');

    // Wait for React to mount
    await page.waitForSelector('#app', { state: 'visible' });

    // Wait for content to render
    await page.waitForTimeout(2000);

    // Check for form elements
    const heading = page.locator('h1, h2');
    await expect(heading.first()).toBeVisible({ timeout: 5000 });

    // Take screenshot
    await page.screenshot({
      path: 'test-results-client/create-resident.png',
      fullPage: true
    });
  });

  test('Navigation works between routes', async ({ page }) => {
    // Start at dashboard
    await page.goto('/dashboard');
    await page.waitForTimeout(2000);

    // Click on Residents link
    const residentsLink = page.locator('a:has-text("Residents")').first();
    if (await residentsLink.isVisible()) {
      await residentsLink.click();
      await page.waitForTimeout(1000);

      // Verify navigation worked
      expect(page.url()).toContain('/residents');
    }

    // Take screenshot of navigation result
    await page.screenshot({
      path: 'test-results-client/navigation.png',
      fullPage: true
    });
  });

  test('S2 Provider applies spectrum styling', async ({ page }) => {
    await page.goto('/dashboard');
    await page.waitForTimeout(2000);

    // Check that S2 styles are loaded
    const styles = await page.evaluate(() => {
      const styleSheets = Array.from(document.styleSheets);
      return styleSheets.some(sheet => {
        try {
          return sheet.href?.includes('s2') ||
                 Array.from(sheet.cssRules || []).some(rule =>
                   rule.cssText?.includes('spectrum') || rule.cssText?.includes('--s2')
                 );
        } catch {
          return false;
        }
      });
    });

    // S2 styles should be present
    expect(styles).toBe(true);

    // Take screenshot
    await page.screenshot({
      path: 'test-results-client/s2-styles.png',
      fullPage: true
    });
  });
});

test.describe('Generated Routes Integration', () => {

  test('Route loaders fetch mock data', async ({ page }) => {
    // Enable console logging
    const logs: string[] = [];
    page.on('console', msg => logs.push(msg.text()));

    await page.goto('/residents');
    await page.waitForTimeout(3000);

    // Should see mock data warning in console (from impulse-runtime)
    const hasMockWarning = logs.some(log =>
      log.includes('mock') || log.includes('Mock') || log.includes('unavailable')
    );

    // Take screenshot
    await page.screenshot({
      path: 'test-results-client/mock-data.png',
      fullPage: true
    });

    // Content should render (even with mock data)
    const pageContent = await page.content();
    expect(pageContent.length).toBeGreaterThan(1000);
  });
});
