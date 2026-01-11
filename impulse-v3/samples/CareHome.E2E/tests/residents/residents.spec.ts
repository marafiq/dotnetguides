import { test, expect } from '../fixtures';

test.describe('Residents Feature', () => {
  test('displays residents list on load', async ({ page }) => {
    await page.goto('/');
    await expect(page.getByTestId('residents-list')).toBeVisible();
    await expect(page.getByText('Residents (3)')).toBeVisible();
    await page.screenshot({ path: 'test-results/screenshots/residents-list.png' });
  });

  test('shows resident data in table', async ({ page }) => {
    await page.goto('/');
    await expect(page.getByTestId('residents-list')).toBeVisible();

    // Check table headers
    await expect(page.getByRole('columnheader', { name: 'Name' })).toBeVisible();
    await expect(page.getByRole('columnheader', { name: 'Room' })).toBeVisible();
    await expect(page.getByRole('columnheader', { name: 'Care Level' })).toBeVisible();
    await expect(page.getByRole('columnheader', { name: 'Age' })).toBeVisible();

    // Check resident data
    await expect(page.getByText('John Smith')).toBeVisible();
    await expect(page.getByText('101A')).toBeVisible();
    await expect(page.getByText('Mary Johnson')).toBeVisible();

    await page.screenshot({ path: 'test-results/screenshots/residents-table.png' });
  });

  test('can navigate to admission wizard', async ({ page }) => {
    await page.goto('/');
    await page.getByTestId('nav-admission').click();
    await expect(page.getByTestId('admission-wizard')).toBeVisible();
    await page.screenshot({ path: 'test-results/screenshots/nav-to-admission.png' });
  });
});
