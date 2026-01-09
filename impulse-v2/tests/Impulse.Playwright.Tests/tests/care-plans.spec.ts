import { test, expect } from '@playwright/test';

/**
 * E2E tests for Care Plans endpoints
 * TDD: These tests define expected behavior BEFORE backend implementation
 *
 * Endpoints tested:
 * - GET /residents/{residentId}/care-plan (GetCarePlan)
 * - POST /residents/{residentId}/care-plan/goals (AddCareGoal)
 * - POST /residents/{residentId}/care-plan/assessments (AddAssessment)
 */
test.describe('Care Plans API', () => {

  test.describe('GET /residents/{residentId}/care-plan - GetCarePlan', () => {
    test('returns care plan with all fields', async ({ request, page }) => {
      const response = await request.get('/residents/1/care-plan', {
        headers: { 'X-Impulse': 'true' },
      });

      expect(response.status()).toBe(200);
      const body = await response.json();

      // Must have GetCarePlanResponse shape
      expect(body.props).toHaveProperty('id');
      expect(body.props).toHaveProperty('residentId');
      expect(body.props).toHaveProperty('residentName');
      expect(body.props).toHaveProperty('effectiveDate');
      expect(body.props).toHaveProperty('assessments');
      expect(body.props).toHaveProperty('goals');
      expect(Array.isArray(body.props.assessments)).toBe(true);
      expect(Array.isArray(body.props.goals)).toBe(true);

      // Screenshot
      await page.goto('/residents/1/care-plan');
      await page.screenshot({ path: 'test-results/care-plan.png' });
    });

    test('returns 404 for resident without care plan', async ({ request }) => {
      const response = await request.get('/residents/999999/care-plan', {
        headers: { 'X-Impulse': 'true' },
      });

      expect(response.status()).toBe(404);
    });

    test('renders care plan page with screenshot', async ({ page }) => {
      await page.goto('/residents/1/care-plan');
      await expect(page.locator('#app')).toBeVisible();
      await page.screenshot({ path: 'test-results/care-plan-html.png' });
    });
  });

  test.describe('POST /residents/{residentId}/care-plan/goals - AddCareGoal', () => {
    test('adds care goal with valid data', async ({ request }) => {
      const response = await request.post('/residents/1/care-plan/goals', {
        headers: {
          'Content-Type': 'application/json',
          'X-Impulse': 'true',
        },
        data: {
          residentId: 1,
          category: 'Mobility',
          description: 'Improve walking distance',
          targetOutcome: 'Walk 100 feet independently',
          targetDate: '2024-06-01',
          interventions: [
            { description: 'Physical therapy 3x weekly', frequency: 'Weekly' },
            { description: 'Daily assisted walks', frequency: 'Daily' },
          ],
        },
      });

      expect([200, 201]).toContain(response.status());
      const body = await response.json();

      // Must have AddCareGoalResponse shape
      expect(body).toHaveProperty('goalId');
      expect(body).toHaveProperty('carePlanId');
      expect(typeof body.goalId).toBe('number');
      expect(typeof body.carePlanId).toBe('number');
    });

    test('returns 422 for missing required fields', async ({ request }) => {
      const response = await request.post('/residents/1/care-plan/goals', {
        headers: {
          'Content-Type': 'application/json',
          'X-Impulse': 'true',
        },
        data: {
          residentId: 1,
          // Missing category, description, targetDate
        },
      });

      expect(response.status()).toBe(422);
      const body = await response.json();
      expect(body).toHaveProperty('errors');
    });
  });

  test.describe('POST /residents/{residentId}/care-plan/assessments - AddAssessment', () => {
    test('adds assessment with valid data', async ({ request }) => {
      const response = await request.post('/residents/1/care-plan/assessments', {
        headers: {
          'Content-Type': 'application/json',
          'X-Impulse': 'true',
        },
        data: {
          residentId: 1,
          type: 'FallRisk',
          assessmentDate: '2024-01-20',
          findings: {
            'gaitStability': 'Moderate impairment',
            'balanceScore': '12/28',
            'medicationReview': 'On sedatives',
          },
          recommendations: [
            'Install bed alarm',
            'Non-slip footwear',
            'Review sedative medications',
          ],
        },
      });

      expect([200, 201]).toContain(response.status());
      const body = await response.json();

      // Must have AddAssessmentResponse shape
      expect(body).toHaveProperty('assessmentId');
      expect(typeof body.assessmentId).toBe('number');
    });

    test('validates assessment type enum', async ({ request }) => {
      const response = await request.post('/residents/1/care-plan/assessments', {
        headers: {
          'Content-Type': 'application/json',
          'X-Impulse': 'true',
        },
        data: {
          residentId: 1,
          type: 'InvalidType', // Invalid enum value
          assessmentDate: '2024-01-20',
        },
      });

      expect(response.status()).toBe(422);
    });

    test('returns 404 for non-existent resident', async ({ request }) => {
      const response = await request.post('/residents/999999/care-plan/assessments', {
        headers: {
          'Content-Type': 'application/json',
          'X-Impulse': 'true',
        },
        data: {
          residentId: 999999,
          type: 'FallRisk',
          assessmentDate: '2024-01-20',
        },
      });

      expect(response.status()).toBe(404);
    });
  });
});
