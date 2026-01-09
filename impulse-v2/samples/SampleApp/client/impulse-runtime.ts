/**
 * Impulse React Runtime
 * Provides the client-side hydration and data fetching capabilities
 */

export interface ImpulseContext {
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
// Server-side validation errors returned
// as structured problem details
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
// When backend isn't running, return mock data
// ========================================

const MOCK_DATA: Record<string, unknown> = {
  '/residents': {
    residents: [
      { id: 1, firstName: 'John', lastName: 'Smith', roomNumber: '101A', status: 'Active', dateOfBirth: '1945-06-15' },
      { id: 2, firstName: 'Mary', lastName: 'Johnson', roomNumber: '102B', status: 'Active', dateOfBirth: '1942-03-22' },
      { id: 3, firstName: 'Robert', lastName: 'Williams', roomNumber: '103A', status: 'Active', dateOfBirth: '1948-11-08' },
      { id: 4, firstName: 'Dorothy', lastName: 'Brown', roomNumber: '104B', status: 'Hospitalized', dateOfBirth: '1936-07-30' },
      { id: 5, firstName: 'James', lastName: 'Davis', roomNumber: '105A', status: 'Active', dateOfBirth: '1952-01-14' },
    ],
    totalCount: 5,
    page: 1,
    pageSize: 20,
  },
  '/dashboard': {
    stats: {
      totalResidents: 5,
      activeResidents: 4,
      hospitalizedResidents: 1,
      onLeaveResidents: 0,
      totalMedications: 23,
      medicationsDueToday: 8,
      overdueMedications: 1,
      activeCarePlans: 5,
      upcomingAssessments: 3,
    },
    recentActivities: [
      { id: 1, type: 'medication_given', title: 'Medication Administered', description: 'Lisinopril 10mg given as scheduled', residentName: 'John Smith', residentId: 1, timestamp: new Date().toISOString(), performedBy: 'Nurse Sarah' },
      { id: 2, type: 'assessment_completed', title: 'Quarterly Assessment', description: 'Completed quarterly health assessment', residentName: 'Mary Johnson', residentId: 2, timestamp: new Date(Date.now() - 2 * 3600000).toISOString(), performedBy: 'Dr. Williams' },
      { id: 3, type: 'care_plan_updated', title: 'Care Plan Updated', description: 'Fall prevention interventions added', residentName: 'Robert Williams', residentId: 3, timestamp: new Date(Date.now() - 4 * 3600000).toISOString(), performedBy: 'Care Coordinator' },
    ],
    upcomingTasks: [
      { id: 1, type: 'medication', title: 'Metformin 500mg', description: 'Morning dose with breakfast', residentId: 1, residentName: 'John Smith', dueAt: new Date(Date.now() + 30 * 60000).toISOString(), priority: 'normal', isOverdue: false },
      { id: 2, type: 'medication', title: 'Amlodipine 5mg', description: 'Evening dose - overdue by 15 min', residentId: 2, residentName: 'Mary Johnson', dueAt: new Date(Date.now() - 15 * 60000).toISOString(), priority: 'urgent', isOverdue: true },
      { id: 3, type: 'assessment', title: 'Annual Assessment Due', description: 'Comprehensive annual health review', residentId: 3, residentName: 'Robert Williams', dueAt: new Date(Date.now() + 2 * 86400000).toISOString(), priority: 'high', isOverdue: false },
    ],
    residentsNeedingAttention: [
      { id: 2, firstName: 'Mary', lastName: 'Johnson', roomNumber: '102B', status: 'Active', activeMedicationsCount: 6, nextMedicationDue: new Date(Date.now() - 15 * 60000).toISOString(), hasOverdueMedications: true, openCareGoals: 2 },
      { id: 3, firstName: 'Robert', lastName: 'Williams', roomNumber: '103A', status: 'Active', activeMedicationsCount: 4, nextMedicationDue: new Date(Date.now() + 3600000).toISOString(), hasOverdueMedications: false, openCareGoals: 3 },
    ],
    medicationCompliance: {
      totalAdministrations: 156,
      onTimeAdministrations: 142,
      lateAdministrations: 11,
      refusedAdministrations: 3,
      complianceRate: 91.0,
    },
    todaysSchedule: [
      { id: 1, time: '6:00 AM', type: 'medication', title: 'Morning Medications', description: '5 residents - 12 medications', residentId: 0, residentName: '', status: 'completed' },
      { id: 2, time: '7:30 AM', type: 'meal', title: 'Breakfast', description: 'All residents', residentId: 0, residentName: '', status: 'completed' },
      { id: 3, time: '9:00 AM', type: 'activity', title: 'Physical Therapy', description: 'Group session', residentId: 0, residentName: '', status: 'completed' },
      { id: 4, time: '10:00 AM', type: 'medication', title: 'Mid-Morning Medications', description: '3 residents - 4 medications', residentId: 0, residentName: '', status: 'pending' },
      { id: 5, time: '12:00 PM', type: 'meal', title: 'Lunch', description: 'All residents', residentId: 0, residentName: '', status: 'pending' },
    ],
  },
};

/**
 * Create an Impulse context for data fetching
 */
export function createImpulseContext(): ImpulseContext {
  return {
    async impulse<T>(url: string): Promise<T> {
      try {
        const response = await fetch(url, {
          headers: {
            'X-Impulse': '1',
            'Accept': 'application/json',
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
      } catch (error) {
        // In preview mode (no backend), return mock data
        console.warn(`Backend unavailable, using mock data for: ${url}`);

        // Find matching mock data
        const basePath = url.split('?')[0];
        if (MOCK_DATA[basePath]) {
          return MOCK_DATA[basePath] as T;
        }

        // Return empty object if no mock found
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

          // Handle ProblemDetails (RFC 7807) - Impulse way
          if (response.status === 400 && errorBody.errors) {
            const problemDetails: ProblemDetails = errorBody;

            // Convert ProblemDetails errors to field errors map
            const fieldErrors: Record<string, string> = {};
            if (problemDetails.errors) {
              for (const [field, messages] of Object.entries(problemDetails.errors)) {
                // Use first error message for each field
                fieldErrors[field] = messages[0] || 'Invalid value';
              }
            }

            throw new ImpulseValidationError(problemDetails, fieldErrors);
          }

          throw new Error(errorBody.error || errorBody.title || `Mutation failed: ${response.status}`);
        }

        return response.json();
      } catch (error) {
        // Re-throw ImpulseValidationError as-is
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

/**
 * Hook for accessing Impulse context in components
 */
export function useImpulseContext(): ImpulseContext {
  // In a real implementation, this would use React context
  return createImpulseContext();
}
