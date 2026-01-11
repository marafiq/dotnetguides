import { test as base, expect } from '@playwright/test';

// Wizard step definitions
const wizardSteps = {
  'basic-info': {
    id: 'basic-info-step',
    title: 'Basic Information',
    description: 'Enter resident basic details',
    fields: [
      { name: 'firstName', label: 'First Name', type: 'text', required: true },
      { name: 'lastName', label: 'Last Name', type: 'text', required: true },
      { name: 'dateOfBirth', label: 'Date of Birth', type: 'date', required: true },
    ],
  },
  'medical-history': {
    id: 'medical-history-step',
    title: 'Medical History',
    description: 'Medical conditions and medications',
    fields: [
      { name: 'conditions', label: 'Medical Conditions', type: 'textarea' },
      { name: 'medications', label: 'Current Medications', type: 'textarea' },
      { name: 'allergies', label: 'Allergies', type: 'textarea' },
    ],
  },
  'care-preferences': {
    id: 'care-preferences-step',
    title: 'Care Preferences',
    description: 'Care level and dietary needs',
    fields: [
      {
        name: 'careLevel',
        label: 'Care Level',
        type: 'select',
        required: true,
        options: [
          { value: 'Independent', label: 'Independent' },
          { value: 'Assisted', label: 'Assisted' },
          { value: 'FullCare', label: 'Full Care' },
          { value: 'Memory', label: 'Memory Care' },
        ],
      },
      {
        name: 'dietaryRequirements',
        label: 'Dietary Requirements',
        type: 'checkboxGroup',
        options: [
          { value: 'Regular', label: 'Regular' },
          { value: 'Diabetic', label: 'Diabetic' },
          { value: 'LowSodium', label: 'LowSodium' },
          { value: 'Vegetarian', label: 'Vegetarian' },
          { value: 'Vegan', label: 'Vegan' },
          { value: 'GlutenFree', label: 'GlutenFree' },
          { value: 'Pureed', label: 'Pureed' },
        ],
      },
      { name: 'specialInstructions', label: 'Special Instructions', type: 'textarea' },
      { name: 'requiresNightChecks', label: 'Requires Night Checks', type: 'checkbox' },
    ],
  },
  'emergency-contacts': {
    id: 'emergency-contacts-step',
    title: 'Emergency Contacts',
    description: 'Emergency contact information',
    fields: [
      { name: 'primaryContactName', label: 'Primary Contact Name', type: 'text', required: true },
      { name: 'primaryContactPhone', label: 'Primary Contact Phone', type: 'text', required: true },
      { name: 'primaryContactRelationship', label: 'Relationship', type: 'text', required: true },
    ],
  },
  'review': {
    id: 'review-step',
    title: 'Review & Submit',
    description: 'Review and submit the admission',
    fields: [
      { name: 'consent', label: 'I consent to the collection and use of this information for care purposes', type: 'checkbox', required: true },
      { name: 'terms', label: 'I accept the terms and conditions', type: 'checkbox', required: true },
    ],
  },
};

const stepOrder = ['basic-info', 'medical-history', 'care-preferences', 'emergency-contacts', 'review'];

// State to track wizard progress (per test)
let wizardFormData: Record<string, unknown> = {};
let currentStepIndex = 0;

// Extended test fixture with API mocking
export const test = base.extend({
  page: async ({ page }, use) => {
    // Reset wizard state for each test
    wizardFormData = {};
    currentStepIndex = 0;

    // Mock home page - returns ResidentsList
    await page.route('**/', async (route) => {
      if (route.request().method() === 'GET') {
        const headers = route.request().headers();
        // Only respond to Impulse requests
        if (headers['x-impulse'] === 'true') {
          await route.fulfill({
            status: 200,
            contentType: 'application/json',
            body: JSON.stringify({
              component: 'ResidentsList',
              props: {
                title: 'Residents',
                residents: [
                  { id: 1, firstName: 'John', lastName: 'Smith', room: '101A', careLevel: 'Independent', age: 78 },
                  { id: 2, firstName: 'Mary', lastName: 'Johnson', room: '102B', careLevel: 'Assisted', age: 85 },
                  { id: 3, firstName: 'Robert', lastName: 'Williams', room: '103A', careLevel: 'FullCare', age: 92 },
                ],
              },
              version: '1.0.0',
            }),
          });
        } else {
          await route.continue();
        }
      } else {
        await route.continue();
      }
    });

    // Mock admission wizard - GET returns current step
    await page.route('**/admission', async (route) => {
      const method = route.request().method();
      const headers = route.request().headers();

      if (method === 'GET' && headers['x-impulse'] === 'true') {
        const stepKey = stepOrder[currentStepIndex];
        const step = wizardSteps[stepKey as keyof typeof wizardSteps];

        await route.fulfill({
          status: 200,
          contentType: 'application/json',
          body: JSON.stringify({
            component: 'AdmissionWizard',
            props: {
              currentStep: currentStepIndex + 1,
              totalSteps: stepOrder.length,
              step,
              formData: wizardFormData,
              submitUrl: '/admission',
              backUrl: currentStepIndex > 0 ? '/admission/back' : undefined,
            },
            version: '1.0.0',
          }),
        });
      } else if (method === 'POST') {
        // Handle form submission
        const body = route.request().postDataJSON();
        Object.assign(wizardFormData, body);

        const stepKey = stepOrder[currentStepIndex];

        // Validate based on current step
        let errors: Record<string, string[]> = {};

        if (stepKey === 'basic-info') {
          if (!body.firstName || body.firstName.length < 2) {
            errors.firstName = ['First name must be at least 2 characters'];
          }
          if (!body.lastName || body.lastName.length < 2) {
            errors.lastName = ['Last name must be at least 2 characters'];
          }
        } else if (stepKey === 'emergency-contacts') {
          if (!body.primaryContactName) {
            errors.primaryContactName = ['Primary contact name is required'];
          }
          if (!body.primaryContactPhone) {
            errors.primaryContactPhone = ['Primary contact phone is required'];
          }
          if (!body.primaryContactRelationship) {
            errors.primaryContactRelationship = ['Relationship is required'];
          }
        } else if (stepKey === 'review') {
          if (!body.consent) {
            errors.consent = ['Consent is required'];
          }
          if (!body.terms) {
            errors.terms = ['Terms must be accepted'];
          }
        }

        if (Object.keys(errors).length > 0) {
          await route.fulfill({
            status: 422,
            contentType: 'application/json',
            body: JSON.stringify({ errors }),
          });
        } else if (currentStepIndex < stepOrder.length - 1) {
          // Move to next step
          currentStepIndex++;
          await route.fulfill({
            status: 200,
            contentType: 'application/json',
            body: JSON.stringify({ redirect: '/admission' }),
          });
        } else {
          // Complete - redirect to home
          await route.fulfill({
            status: 200,
            contentType: 'application/json',
            body: JSON.stringify({ redirect: '/' }),
          });
        }
      } else {
        await route.continue();
      }
    });

    // Mock back navigation - returns previous step directly
    await page.route('**/admission/back', async (route) => {
      const headers = route.request().headers();
      if (headers['x-impulse'] === 'true') {
        if (currentStepIndex > 0) {
          currentStepIndex--;
        }
        const stepKey = stepOrder[currentStepIndex];
        const step = wizardSteps[stepKey as keyof typeof wizardSteps];

        await route.fulfill({
          status: 200,
          contentType: 'application/json',
          body: JSON.stringify({
            component: 'AdmissionWizard',
            props: {
              currentStep: currentStepIndex + 1,
              totalSteps: stepOrder.length,
              step,
              formData: wizardFormData,
              submitUrl: '/admission',
              backUrl: currentStepIndex > 0 ? '/admission/back' : undefined,
            },
            version: '1.0.0',
          }),
        });
      } else {
        await route.continue();
      }
    });

    await use(page);
  },
});

export { expect };
