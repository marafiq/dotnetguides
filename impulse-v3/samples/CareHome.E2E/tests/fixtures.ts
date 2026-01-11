import { test as base, expect } from '@playwright/test';

// Mock API responses
export const mockResponses = {
  residents: {
    props: {
      residents: [
        { id: 1, fullName: 'John Smith', roomNumber: '101A', careLevel: 'Independent', age: 78 },
        { id: 2, fullName: 'Mary Johnson', roomNumber: '102B', careLevel: 'Assisted', age: 85 },
        { id: 3, fullName: 'Robert Williams', roomNumber: '103A', careLevel: 'FullCare', age: 92 },
      ],
      totalCount: 3,
    },
    component: '/residents',
    version: '1.0.0',
  },
  admissionWizard: {
    props: {
      steps: [
        { id: 'basic-info', title: 'Basic Information', description: 'Enter resident basic details' },
        { id: 'medical-history', title: 'Medical History', description: 'Medical conditions and medications' },
        { id: 'care-preferences', title: 'Care Preferences', description: 'Care level and dietary needs' },
        { id: 'emergency-contacts', title: 'Emergency Contacts', description: 'Emergency contact information' },
        { id: 'review', title: 'Review & Submit', description: 'Review and submit the admission' },
      ],
      careLevels: ['Independent', 'Assisted', 'FullCare', 'Memory'],
      dietaryRequirements: ['Regular', 'Diabetic', 'LowSodium', 'Vegetarian', 'Vegan', 'GlutenFree', 'Pureed'],
    },
    component: '/admission/wizard',
    version: '1.0.0',
  },
  validateBasicInfoSuccess: { isValid: true, errors: null },
  validateBasicInfoError: {
    isValid: false,
    errors: {
      firstName: ['First name must be at least 2 characters'],
      lastName: ['Last name must be at least 2 characters'],
      dateOfBirth: ['Resident must be at least 55 years old'],
    },
  },
  validateEmergencyContactsError: {
    isValid: false,
    errors: {
      primaryContactName: ['Primary contact name is required'],
      primaryContactPhone: ['Primary contact phone is required'],
      primaryContactRelationship: ['Relationship is required'],
    },
  },
  completeAdmissionSuccess: {
    residentId: 123,
    roomAssigned: '205B',
    message: 'Welcome to Green Valley Care Home! Room 205B has been assigned.',
  },
  completeAdmissionError: {
    errors: {
      consentGiven: ['Consent is required'],
      termsAccepted: ['Terms must be accepted'],
    },
  },
};

// Extended test fixture with API mocking
export const test = base.extend({
  page: async ({ page }, use) => {
    // Mock all API endpoints
    await page.route('**/residents', async (route) => {
      if (route.request().method() === 'GET') {
        await route.fulfill({
          status: 200,
          contentType: 'application/json',
          body: JSON.stringify(mockResponses.residents),
        });
      } else {
        await route.continue();
      }
    });

    await page.route('**/admission/wizard', async (route) => {
      if (route.request().method() === 'GET') {
        await route.fulfill({
          status: 200,
          contentType: 'application/json',
          body: JSON.stringify(mockResponses.admissionWizard),
        });
      }
    });

    await page.route('**/admission/wizard/validate/basic-info', async (route) => {
      const body = route.request().postDataJSON();
      const isValid = body?.firstName?.length >= 2 && body?.lastName?.length >= 2 && body?.dateOfBirth;
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify(isValid ? mockResponses.validateBasicInfoSuccess : mockResponses.validateBasicInfoError),
      });
    });

    await page.route('**/admission/wizard/validate/medical-history', async (route) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({ isValid: true, errors: null }),
      });
    });

    await page.route('**/admission/wizard/validate/care-preferences', async (route) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({ isValid: true, errors: null }),
      });
    });

    await page.route('**/admission/wizard/validate/emergency-contacts', async (route) => {
      const body = route.request().postDataJSON();
      const isValid = body?.primaryContactName && body?.primaryContactPhone && body?.primaryContactRelationship;
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify(isValid ? { isValid: true, errors: null } : mockResponses.validateEmergencyContactsError),
      });
    });

    await page.route('**/admission/wizard/complete', async (route) => {
      const body = route.request().postDataJSON();
      if (body?.consentGiven && body?.termsAccepted) {
        await route.fulfill({
          status: 200,
          contentType: 'application/json',
          body: JSON.stringify(mockResponses.completeAdmissionSuccess),
        });
      } else {
        await route.fulfill({
          status: 422,
          contentType: 'application/json',
          body: JSON.stringify(mockResponses.completeAdmissionError),
        });
      }
    });

    await use(page);
  },
});

export { expect };
