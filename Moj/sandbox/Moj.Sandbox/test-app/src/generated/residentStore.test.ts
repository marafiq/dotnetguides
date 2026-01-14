import { describe, it, expect } from 'vitest';
import {
  residentStore,
  // Actions
  setResidents,
  addResident,
  updateResident,
  removeResident,
  selectResident,
  startLoading,
  setError,
  clearError,
  setFilters,
  setStatusFilter,
  setCareLevelFilter,
  setSearchQuery,
  clearFilters,
  toggleSidebar,
  setActiveTab,
  setViewMode,
  setPage,
  toggleAlertPanel,
  toggleActivityCalendar,
  addAlert,
  acknowledgeAlert,
  setStaff,
  setRooms,
  setActivities,
  setCurrentUser,
  // Selectors
  totalResidents,
  activeResidents,
  criticalAlerts,
  availableRooms,
  onDutyStaff,
  hasUnacknowledgedAlerts,
  // Types
  type ResidentAppState,
  type Resident,
  type Room,
  type StaffMember,
  type ResidentAlert,
  type FacilityActivity,
  type ResidentFilters,
  type ResidentStatus,
  type CareLevel,
  type AlertSeverity,
} from './residentStore';

describe('ResidentStore - Typed DSL Generated', () => {
  describe('Store Initialization', () => {
    it('should create store with initial state', () => {
      const state = residentStore.state;
      expect(state).toBeDefined();
      expect(state.residents).toEqual([]);
      expect(state.selectedResident).toBeNull();
      expect(state.isLoading).toBe(false);
      expect(state.error).toBeNull();
    });

    it('should have UI state initialized correctly', () => {
      const state = residentStore.state;
      expect(state.ui).toBeDefined();
      expect(state.ui?.sidebarOpen).toBe(true);
      expect(state.ui?.activeTab).toBe('overview');
      expect(state.ui?.viewMode).toBe('list');
    });

    it('should have filters initialized correctly', () => {
      const state = residentStore.state;
      expect(state.filters).toBeDefined();
      expect(state.filters?.statuses).toEqual([]);
      expect(state.filters?.careLevels).toEqual([]);
    });

    it('should have pagination state initialized', () => {
      const state = residentStore.state;
      expect(state.ui?.pagination).toBeDefined();
      expect(state.ui?.pagination?.page).toBe(1);
      expect(state.ui?.pagination?.pageSize).toBe(25);
    });
  });

  describe('Type Exports', () => {
    it('should export ResidentStatus type', () => {
      const status: ResidentStatus = 'active';
      expect(['active', 'temporary', 'onLeave', 'hospitalized', 'discharged', 'deceased']).toContain(status);
    });

    it('should export CareLevel type', () => {
      const careLevel: CareLevel = 'assistedLiving';
      expect(['independent', 'assistedLiving', 'memoryCare', 'skilledNursing', 'hospice']).toContain(careLevel);
    });

    it('should export AlertSeverity type', () => {
      const severity: AlertSeverity = 'critical';
      expect(['info', 'low', 'medium', 'high', 'critical']).toContain(severity);
    });
  });

  describe('Interface Exports', () => {
    it('should export Resident interface', () => {
      const resident: Resident = {
        id: 'res-1',
        firstName: 'John',
        lastName: 'Doe',
        dateOfBirth: '1940-01-01',
        status: 'active',
        careLevel: 'assistedLiving',
        admissionDate: '2024-01-01',
        createdAt: new Date().toISOString(),
        updatedAt: new Date().toISOString(),
      };
      expect(resident.id).toBe('res-1');
      expect(resident.status).toBe('active');
    });

    it('should export Room interface', () => {
      const room: Room = {
        id: 'room-101',
        number: '101',
        floor: 1,
        building: 'Main',
        type: 'oneBedroom',
        capacity: 1,
        monthlyRate: 5000,
        isAvailable: true,
      };
      expect(room.type).toBe('oneBedroom');
      expect(room.isAvailable).toBe(true);
    });

    it('should export StaffMember interface', () => {
      const staff: StaffMember = {
        id: 'staff-1',
        firstName: 'Jane',
        lastName: 'Smith',
        role: 'nurse',
        isOnDuty: true,
      };
      expect(staff.role).toBe('nurse');
      expect(staff.isOnDuty).toBe(true);
    });

    it('should export ResidentAlert interface', () => {
      const alert: ResidentAlert = {
        id: 'alert-1',
        residentId: 'res-1',
        type: 'fall_risk',
        severity: 'high',
        message: 'Increased fall risk',
        createdAt: new Date().toISOString(),
        isResolved: false,
      };
      expect(alert.severity).toBe('high');
      expect(alert.isResolved).toBe(false);
    });

    it('should export FacilityActivity interface', () => {
      const activity: FacilityActivity = {
        id: 'act-1',
        name: 'Morning Exercise',
        type: 'physical',
        startTime: new Date().toISOString(),
        endTime: new Date().toISOString(),
        maxParticipants: 20,
        isRecurring: true,
      };
      expect(activity.type).toBe('physical');
      expect(activity.isRecurring).toBe(true);
    });
  });

  describe('Action Exports', () => {
    it('should export setResidents action', () => {
      expect(typeof setResidents).toBe('function');
    });

    it('should export addResident action', () => {
      expect(typeof addResident).toBe('function');
    });

    it('should export updateResident action', () => {
      expect(typeof updateResident).toBe('function');
    });

    it('should export removeResident action', () => {
      expect(typeof removeResident).toBe('function');
    });

    it('should export selectResident action', () => {
      expect(typeof selectResident).toBe('function');
    });

    it('should export loading actions', () => {
      expect(typeof startLoading).toBe('function');
      expect(typeof setError).toBe('function');
      expect(typeof clearError).toBe('function');
    });

    it('should export filter actions', () => {
      expect(typeof setFilters).toBe('function');
      expect(typeof setStatusFilter).toBe('function');
      expect(typeof setCareLevelFilter).toBe('function');
      expect(typeof setSearchQuery).toBe('function');
      expect(typeof clearFilters).toBe('function');
    });

    it('should export UI actions', () => {
      expect(typeof toggleSidebar).toBe('function');
      expect(typeof setActiveTab).toBe('function');
      expect(typeof setViewMode).toBe('function');
      expect(typeof setPage).toBe('function');
      expect(typeof toggleAlertPanel).toBe('function');
      expect(typeof toggleActivityCalendar).toBe('function');
    });

    it('should export alert actions', () => {
      expect(typeof addAlert).toBe('function');
      expect(typeof acknowledgeAlert).toBe('function');
    });

    it('should export staff/room/activity actions', () => {
      expect(typeof setStaff).toBe('function');
      expect(typeof setRooms).toBe('function');
      expect(typeof setActivities).toBe('function');
      expect(typeof setCurrentUser).toBe('function');
    });
  });

  describe('Selector Exports', () => {
    it('should export totalResidents selector', () => {
      expect(typeof totalResidents).toBe('function');
    });

    it('should export activeResidents selector', () => {
      expect(typeof activeResidents).toBe('function');
    });

    it('should export criticalAlerts selector', () => {
      expect(typeof criticalAlerts).toBe('function');
    });

    it('should export availableRooms selector', () => {
      expect(typeof availableRooms).toBe('function');
    });

    it('should export onDutyStaff selector', () => {
      expect(typeof onDutyStaff).toBe('function');
    });

    it('should export hasUnacknowledgedAlerts selector', () => {
      expect(typeof hasUnacknowledgedAlerts).toBe('function');
    });
  });

  describe('Complex Type Structures', () => {
    it('should handle nested Resident with Room', () => {
      const resident: Resident = {
        id: 'res-2',
        firstName: 'Alice',
        lastName: 'Johnson',
        dateOfBirth: '1935-05-15',
        status: 'active',
        careLevel: 'memoryCare',
        admissionDate: '2023-06-01',
        room: {
          id: 'room-201',
          number: '201',
          floor: 2,
          building: 'Memory Care Wing',
          type: 'privateMemoryCare',
          capacity: 1,
          monthlyRate: 8500,
          isAvailable: false,
          currentResidentId: 'res-2',
        },
        createdAt: new Date().toISOString(),
        updatedAt: new Date().toISOString(),
      };
      expect(resident.room?.type).toBe('privateMemoryCare');
      expect(resident.room?.currentResidentId).toBe('res-2');
    });

    it('should handle Resident with emergency contacts', () => {
      const resident: Resident = {
        id: 'res-3',
        firstName: 'Bob',
        lastName: 'Williams',
        dateOfBirth: '1942-08-20',
        status: 'active',
        careLevel: 'skilledNursing',
        admissionDate: '2024-02-15',
        emergencyContacts: [
          {
            id: 'ec-1',
            name: 'Mary Williams',
            relationship: 'Spouse',
            isPrimaryContact: true,
            hasPowerOfAttorney: true,
            isHealthcareProxy: true,
            contact: {
              phone: '555-0123',
              email: 'mary@example.com',
            },
          },
        ],
        createdAt: new Date().toISOString(),
        updatedAt: new Date().toISOString(),
      };
      expect(resident.emergencyContacts?.length).toBe(1);
      expect(resident.emergencyContacts?.[0].isPrimaryContact).toBe(true);
    });

    it('should handle ResidentFilters with enum arrays', () => {
      const filters: ResidentFilters = {
        statuses: ['active', 'hospitalized'],
        careLevels: ['memoryCare', 'skilledNursing'],
        building: 'Main',
        hasActiveAlerts: true,
      };
      expect(filters.statuses?.length).toBe(2);
      expect(filters.careLevels?.includes('memoryCare')).toBe(true);
    });
  });

  describe('State Shape Validation', () => {
    it('should have correct ResidentAppState shape', () => {
      const state: ResidentAppState = {
        residents: [],
        selectedResident: undefined,
        rooms: [],
        staff: [],
        activities: [],
        unacknowledgedAlerts: [],
        currentUser: undefined,
        isLoading: false,
        error: undefined,
        filters: {
          statuses: [],
          careLevels: [],
        },
        ui: {
          sidebarOpen: true,
          activeTab: 'overview',
          viewMode: 'list',
          showAlertPanel: false,
          showActivityCalendar: false,
        },
      };
      expect(state.isLoading).toBe(false);
      expect(state.ui?.sidebarOpen).toBe(true);
    });
  });
});

describe('ResidentStore - Enum Type Unions', () => {
  it('should allow all valid ResidentStatus values', () => {
    const validStatuses: ResidentStatus[] = [
      'active',
      'temporary',
      'onLeave',
      'hospitalized',
      'discharged',
      'deceased',
    ];
    validStatuses.forEach(status => {
      const resident: Partial<Resident> = { status };
      expect(resident.status).toBe(status);
    });
  });

  it('should allow all valid CareLevel values', () => {
    const validLevels: CareLevel[] = [
      'independent',
      'assistedLiving',
      'memoryCare',
      'skilledNursing',
      'hospice',
    ];
    validLevels.forEach(level => {
      const resident: Partial<Resident> = { careLevel: level };
      expect(resident.careLevel).toBe(level);
    });
  });

  it('should allow all valid AlertSeverity values', () => {
    const validSeverities: AlertSeverity[] = [
      'info',
      'low',
      'medium',
      'high',
      'critical',
    ];
    validSeverities.forEach(severity => {
      const alert: Partial<ResidentAlert> = { severity };
      expect(alert.severity).toBe(severity);
    });
  });
});
