import { test, expect } from '@playwright/test';

test.describe('Pane - Slide-out Detail Views', () => {
  test('displays resident detail pane', async ({ page }) => {
    await page.goto('/pane/residents/1');

    // Verify pane is visible
    await expect(page.locator('.pane-overlay')).toBeVisible();
    await expect(page.locator('.pane')).toBeVisible();

    // Verify pane header
    await expect(page.locator('#pane-title')).toBeVisible();
    await expect(page.locator('.pane-subtitle')).toBeVisible();

    await page.screenshot({ path: 'screenshots/pane-resident-detail.png', fullPage: true });
  });

  test('pane has close button', async ({ page }) => {
    await page.goto('/pane/residents/1');

    await expect(page.locator('.pane-close')).toBeVisible();

    await page.screenshot({ path: 'screenshots/pane-close-button.png', fullPage: true });
  });

  test('pane displays sections', async ({ page }) => {
    await page.goto('/pane/residents/1');

    // Verify sections
    await expect(page.locator('.pane-section')).toBeVisible();
    await expect(page.locator('.pane-section-title')).toBeVisible();

    await page.screenshot({ path: 'screenshots/pane-sections.png', fullPage: true });
  });

  test('pane displays section items', async ({ page }) => {
    await page.goto('/pane/residents/1');

    // Verify section items (dl/dt/dd structure)
    await expect(page.locator('.pane-section-items')).toBeVisible();
    await expect(page.locator('dt').first()).toBeVisible();
    await expect(page.locator('dd').first()).toBeVisible();

    await page.screenshot({ path: 'screenshots/pane-section-items.png', fullPage: true });
  });

  test('pane has footer actions', async ({ page }) => {
    await page.goto('/pane/residents/1');

    // Verify footer
    await expect(page.locator('.pane-footer')).toBeVisible();
    await expect(page.locator('.pane-footer button')).toBeVisible();

    await page.screenshot({ path: 'screenshots/pane-footer-actions.png', fullPage: true });
  });

  test('displays filter pane', async ({ page }) => {
    await page.goto('/residents/filter');

    // Verify filter pane content
    await expect(page.locator('.filter-pane, .pane')).toBeVisible();
    await expect(page.locator('h2')).toContainText('Filter');

    // Verify filter groups
    await expect(page.locator('fieldset')).toBeVisible();

    await page.screenshot({ path: 'screenshots/pane-filter.png', fullPage: true });
  });

  test('filter pane has checkbox options', async ({ page }) => {
    await page.goto('/residents/filter');

    // Verify checkbox filters
    await expect(page.locator('input[type="checkbox"]').first()).toBeVisible();

    await page.screenshot({ path: 'screenshots/pane-filter-checkboxes.png', fullPage: true });
  });

  test('filter pane - select filters', async ({ page }) => {
    await page.goto('/residents/filter');

    // Check some filter options
    const checkboxes = page.locator('input[type="checkbox"]');
    if (await checkboxes.count() > 0) {
      await checkboxes.first().check();
    }

    await page.screenshot({ path: 'screenshots/pane-filter-selected.png', fullPage: true });
  });

  test('filter pane has action buttons', async ({ page }) => {
    await page.goto('/residents/filter');

    // Verify action buttons
    await expect(page.locator('button:has-text("Reset"), button:has-text("Apply")')).toBeVisible();

    await page.screenshot({ path: 'screenshots/pane-filter-actions.png', fullPage: true });
  });

  test('displays activity feed pane', async ({ page }) => {
    await page.goto('/activity');

    // Verify activity feed content
    await expect(page.locator('.activity-feed-pane, .feed-title')).toBeVisible();

    await page.screenshot({ path: 'screenshots/pane-activity-feed.png', fullPage: true });
  });

  test('activity feed shows activity items', async ({ page }) => {
    await page.goto('/activity');

    // Verify activity list
    await expect(page.locator('.activity-list, .activity-item, li').first()).toBeVisible();

    await page.screenshot({ path: 'screenshots/pane-activity-items.png', fullPage: true });
  });

  test('activity feed has load more button', async ({ page }) => {
    await page.goto('/activity');

    // Verify load more button
    await expect(page.locator('button:has-text("Load More")')).toBeVisible();

    await page.screenshot({ path: 'screenshots/pane-activity-load-more.png', fullPage: true });
  });
});
