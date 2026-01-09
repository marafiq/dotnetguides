import { test, expect } from '@playwright/test';

/**
 * E2E tests for Medications endpoints
 * TDD: These tests define expected behavior BEFORE backend implementation
 *
 * Endpoints tested:
 * - GET /residents/{residentId}/medications (ListMedications)
 * - GET /residents/{residentId}/medications/{medicationId} (GetMedication)
 * - POST /residents/{residentId}/medications (AddMedication)
 * - POST /residents/{residentId}/medications/{medicationId}/administer (RecordAdministration)
 */
test.describe('Medications API', () => {

  test.describe('GET /residents/{residentId}/medications - ListMedications', () => {
    test('returns medications list for resident', async ({ request, page }) => {
      const response = await request.get('/residents/1/medications', {
        headers: { 'X-Impulse': 'true' },
      });

      expect(response.status()).toBe(200);
      const body = await response.json();

      // Must have ListMedicationsResponse shape
      expect(body.props).toHaveProperty('residentId');
      expect(body.props).toHaveProperty('medications');
      expect(Array.isArray(body.props.medications)).toBe(true);

      // Screenshot
      await page.goto('/residents/1/medications');
      await page.screenshot({ path: 'test-results/medications-list.png' });
    });

    test('filters discontinued medications when includeDiscontinued=false', async ({ request }) => {
      const response = await request.get('/residents/1/medications?includeDiscontinued=false', {
        headers: { 'X-Impulse': 'true' },
      });

      expect(response.status()).toBe(200);
      const body = await response.json();

      // All returned medications should NOT have discontinued status
      for (const med of body.props.medications || []) {
        expect(med.status).not.toBe('Discontinued');
      }
    });
  });

  test.describe('GET /residents/{residentId}/medications/{medicationId} - GetMedication', () => {
    test('returns medication detail with all fields', async ({ request }) => {
      const response = await request.get('/residents/1/medications/1', {
        headers: { 'X-Impulse': 'true' },
      });

      expect(response.status()).toBe(200);
      const body = await response.json();

      // Must have GetMedicationResponse shape
      expect(body.props).toHaveProperty('id');
      expect(body.props).toHaveProperty('residentId');
      expect(body.props).toHaveProperty('drugName');
      expect(body.props).toHaveProperty('route');
      expect(body.props).toHaveProperty('status');
    });

    test('returns 404 for non-existent medication', async ({ request }) => {
      const response = await request.get('/residents/1/medications/999999', {
        headers: { 'X-Impulse': 'true' },
      });

      expect(response.status()).toBe(404);
    });

    test('renders medication detail page with screenshot', async ({ page }) => {
      await page.goto('/residents/1/medications/1');
      await expect(page.locator('#app')).toBeVisible();
      await page.screenshot({ path: 'test-results/medication-detail.png' });
    });
  });

  test.describe('POST /residents/{residentId}/medications - AddMedication', () => {
    test('adds new medication with valid data', async ({ request }) => {
      const response = await request.post('/residents/1/medications', {
        headers: {
          'Content-Type': 'application/json',
          'X-Impulse': 'true',
        },
        data: {
          residentId: 1,
          drugName: 'Lisinopril',
          genericName: 'Lisinopril',
          prescribedDosage: {
            amount: 10,
            unit: 'mg',
            instructions: 'Take with water',
          },
          route: 'Oral',
          administrationTimes: ['Morning'],
          frequencyDescription: 'Once daily',
          startDate: new Date().toISOString().split('T')[0], // Today
          prescriber: 'Dr. Smith',
          purpose: 'Blood pressure management',
        },
      });

      expect([200, 201]).toContain(response.status());
      const body = await response.json();

      // Must have AddMedicationResponse shape
      expect(body).toHaveProperty('medicationId');
      expect(typeof body.medicationId).toBe('number');
    });

    test('returns 422 for invalid data', async ({ request }) => {
      const response = await request.post('/residents/1/medications', {
        headers: {
          'Content-Type': 'application/json',
          'X-Impulse': 'true',
        },
        data: {
          residentId: 1,
          drugName: '', // Empty - fails validation
          prescribedDosage: { amount: 0, unit: '', instructions: null }, // Invalid dosage
          route: 'Oral',
          administrationTimes: [], // Empty - fails validation
          startDate: '2020-01-01', // Too old - fails validation
          prescriber: '', // Empty - fails validation
        },
      });

      expect(response.status()).toBe(422);
      const body = await response.json();
      expect(body).toHaveProperty('errors');
    });
  });

  test.describe('POST /residents/{residentId}/medications/{medicationId}/administer - RecordAdministration', () => {
    test('records medication administration', async ({ request }) => {
      const response = await request.post('/residents/1/medications/1/administer', {
        headers: {
          'Content-Type': 'application/json',
          'X-Impulse': 'true',
        },
        data: {
          residentId: 1,
          medicationId: 1,
          administeredAt: new Date().toISOString(),
          dosageGiven: {
            amount: 10,
            unit: 'mg',
            instructions: null,
          },
          wasRefused: false,
          notes: 'Administered without issues',
        },
      });

      expect([200, 201]).toContain(response.status());
      const body = await response.json();

      // Must have RecordAdministrationResponse shape
      expect(body).toHaveProperty('administrationId');
      expect(typeof body.administrationId).toBe('number');
    });

    test('records refusal with reason', async ({ request }) => {
      const response = await request.post('/residents/1/medications/1/administer', {
        headers: {
          'Content-Type': 'application/json',
          'X-Impulse': 'true',
        },
        data: {
          residentId: 1,
          medicationId: 1,
          administeredAt: new Date().toISOString(),
          wasRefused: true,
          refusalReason: 'Resident was nauseous',
        },
      });

      expect([200, 201]).toContain(response.status());
      const body = await response.json();
      expect(body).toHaveProperty('administrationId');
    });
  });
});
