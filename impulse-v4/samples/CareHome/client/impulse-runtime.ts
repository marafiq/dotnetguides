/**
 * Impulse React Runtime
 * Provides the client-side hydration and data fetching capabilities
 */

export interface ImpulseContextValue {
  /** Fetch data from an Impulse endpoint */
  impulse: <T>(url: string) => Promise<T>;
  /** Execute a mutation against an Impulse endpoint */
  impulseMutate: <TReq, TRes>(url: string, data: TReq, method: string) => Promise<TRes>;
}

export interface ImpulseData<T> {
  props: T;
  component: string;
  version: string;
}

// ========================================
// ProblemDetails Error (RFC 7807)
// ========================================

export interface ProblemDetails {
  type?: string;
  title?: string;
  status?: number;
  detail?: string;
  instance?: string;
  errors?: Record<string, string[]>;
}

export class ImpulseValidationError extends Error {
  constructor(
    public readonly problemDetails: ProblemDetails,
    public readonly fieldErrors: Record<string, string>
  ) {
    super(problemDetails.title || 'Validation failed');
    this.name = 'ImpulseValidationError';
  }
}

// ========================================
// Mock Data for Preview Mode
// ========================================

const MOCK_DATA: Record<string, unknown> = {
  '/': {
    stats: {
      totalResidents: 3,
      activeResidents: 3,
      newAdmissionsThisMonth: 1,
      upcomingBirthdays: 2,
    },
    recentAdmissions: [
      { id: 1, firstName: 'John', lastName: 'Smith', roomNumber: '101A', careLevel: 'Independent', age: 78, status: 'Active' },
    ],
  },
  '/residents': {
    residents: [
      { id: 1, firstName: 'John', lastName: 'Smith', roomNumber: '101A', careLevel: 'Independent', age: 78, status: 'Active' },
      { id: 2, firstName: 'Mary', lastName: 'Johnson', roomNumber: '102B', careLevel: 'Assisted', age: 85, status: 'Active' },
      { id: 3, firstName: 'Robert', lastName: 'Williams', roomNumber: '103A', careLevel: 'FullCare', age: 92, status: 'Active' },
    ],
    totalCount: 3,
    page: 1,
    pageSize: 20,
  },
  '/admission/wizard': {
    wizardId: 'mock-wizard-id',
    title: 'Resident Admission',
    description: 'Complete the following steps to admit a new resident.',
    steps: [
      { stepNumber: 1, id: 'basic-info', title: 'Basic Information', description: 'Enter resident personal info', isRequired: true, isComplete: false, fields: [] },
      { stepNumber: 2, id: 'medical-history', title: 'Medical History', description: 'Document medical info', isRequired: true, isComplete: false, fields: [] },
      { stepNumber: 3, id: 'care-preferences', title: 'Care Preferences', description: 'Set care preferences', isRequired: true, isComplete: false, fields: [] },
      { stepNumber: 4, id: 'emergency-contacts', title: 'Emergency Contacts', description: 'Add emergency contacts', isRequired: true, isComplete: false, fields: [] },
      { stepNumber: 5, id: 'review', title: 'Review & Confirm', description: 'Review and complete', isRequired: true, isComplete: false, fields: [] },
    ],
    currentStep: 1,
    totalSteps: 5,
  },
};

/**
 * Create an Impulse context for data fetching
 */
export function createImpulseContext(): ImpulseContextValue {
  return {
    async impulse<T>(url: string): Promise<T> {
      try {
        const response = await fetch(url, {
          headers: {
            'X-Impulse': '1',
            Accept: 'application/json',
          },
        });

        if (!response.ok) {
          throw new Error(`Failed to fetch ${url}: ${response.status}`);
        }

        const data: ImpulseData<T> = await response.json();

        // Check for version mismatch - reload if server version changed
        if (response.headers.get('X-Impulse-Reload') === 'true') {
          window.location.reload();
        }

        return data.props;
      } catch {
        // In preview mode (no backend), return mock data
        console.warn(`Backend unavailable, using mock data for: ${url}`);

        const basePath = url.split('?')[0];
        if (MOCK_DATA[basePath]) {
          return MOCK_DATA[basePath] as T;
        }

        console.warn(`No mock data for: ${basePath}`);
        return {} as T;
      }
    },

    async impulseMutate<TReq, TRes>(
      url: string,
      data: TReq,
      method: string
    ): Promise<TRes> {
      try {
        const response = await fetch(url, {
          method,
          headers: {
            'Content-Type': 'application/json',
            'X-Impulse': '1',
          },
          body: JSON.stringify(data),
        });

        if (!response.ok) {
          const errorBody = await response.json();

          // Handle ProblemDetails (RFC 7807)
          if (response.status === 400 && errorBody.errors) {
            const problemDetails: ProblemDetails = errorBody;
            const fieldErrors: Record<string, string> = {};

            if (problemDetails.errors) {
              for (const [field, messages] of Object.entries(problemDetails.errors)) {
                fieldErrors[field] = messages[0] || 'Invalid value';
              }
            }

            throw new ImpulseValidationError(problemDetails, fieldErrors);
          }

          throw new Error(errorBody.error || errorBody.title || `Mutation failed: ${response.status}`);
        }

        return response.json();
      } catch (error) {
        if (error instanceof ImpulseValidationError) {
          throw error;
        }

        // In preview mode, return mock success
        console.warn(`Backend unavailable, mocking mutation for: ${url}`);
        return { id: Math.floor(Math.random() * 1000) } as TRes;
      }
    },
  };
}

/**
 * Extract initial props from SSR hydration data
 */
export function getHydrationData<T>(): ImpulseData<T> | null {
  const appElement = document.getElementById('app');
  if (!appElement) return null;

  const dataAttr = appElement.getAttribute('data-impulse');
  if (!dataAttr) return null;

  try {
    return JSON.parse(dataAttr);
  } catch {
    console.error('Failed to parse Impulse hydration data');
    return null;
  }
}
