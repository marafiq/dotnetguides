import { test, expect } from '@playwright/test';

test('debug dashboard hydration', async ({ page }) => {
  // Capture console messages
  const consoleMessages: string[] = [];
  page.on('console', msg => {
    consoleMessages.push('[' + msg.type() + '] ' + msg.text());
  });

  // Capture page errors
  const pageErrors: string[] = [];
  page.on('pageerror', error => {
    pageErrors.push(error.message);
  });

  await page.goto('/dashboard');

  // Wait for scripts to load
  await page.waitForTimeout(3000);

  // Get the HTML content
  const html = await page.content();

  // Check if JS bundle loaded
  const jsLoaded = html.includes('<script');

  // Check data attributes
  const appElement = page.locator('#app');
  const dataComponent = await appElement.getAttribute('data-component');
  const dataImpulse = await appElement.getAttribute('data-impulse');

  // Check if React rendered anything
  const innerHtml = await appElement.innerHTML();

  console.log('=== DEBUG INFO ===');
  console.log('JS bundle in HTML:', jsLoaded);
  console.log('data-component:', dataComponent);
  console.log('data-impulse exists:', !!dataImpulse);
  console.log('App innerHTML length:', innerHtml.length);
  console.log('App innerHTML preview:', innerHtml.substring(0, 500));
  console.log('=== CONSOLE MESSAGES ===');
  consoleMessages.forEach(m => console.log(m));
  console.log('=== PAGE ERRORS ===');
  pageErrors.forEach(e => console.log(e));

  // Take screenshot
  await page.screenshot({ path: 'test-results/debug-dashboard.png', fullPage: true });

  // Expect React to have rendered content
  expect(innerHtml.length).toBeGreaterThan(10);
});
