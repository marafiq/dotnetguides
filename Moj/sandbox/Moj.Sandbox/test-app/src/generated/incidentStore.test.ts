/**
 * Tests to validate the generated TanStack Store code actually works
 */
import { describe, it, expect, beforeEach } from 'vitest';
import {
  incidentStore,
  setIncidents,
  addIncident,
  updateIncident,
  removeIncident,
  selectIncident,
  setCurrentUser,
  startLoading,
  setError,
  clearError,
  setStatusFilter,
  setPriorityFilter,
  setSearchQuery,
  clearFilters,
  toggleSidebar,
  setActiveView,
  setSort,
  setPage,
  // Selectors
  totalIncidents,
  openIncidents,
  criticalIncidents,
  filteredIncidents,
  incidentCountByStatus,
  hasActiveFilters,
  isSelectedIncidentResolved
} from './incidentStore';
import type { Incident, User } from './types';

// Test data
const mockUser: User = {
  id: 'user-1',
  name: 'John Doe',
  email: 'john@example.com',
  department: 'Engineering',
  roles: ['developer']
};

const mockIncident: Incident = {
  id: 'inc-1',
  title: 'Critical Bug',
  description: 'Something is broken',
  status: 'open',
  priority: 'critical',
  category: 'bug',
  assignedTo: mockUser,
  reporter: mockUser,
  watchers: [],
  labels: ['urgent', 'frontend'],
  comments: [],
  attachments: [],
  relatedIncidents: [],
  customFields: {},
  metadata: {
    createdAt: '2024-01-15T10:00:00Z',
    updatedAt: '2024-01-15T10:00:00Z',
    createdBy: 'user-1',
    viewCount: 5
  }
};

const mockIncident2: Incident = {
  ...mockIncident,
  id: 'inc-2',
  title: 'Low Priority Task',
  status: 'inProgress',
  priority: 'low',
  category: 'feature'
};

const mockIncident3: Incident = {
  ...mockIncident,
  id: 'inc-3',
  title: 'Resolved Issue',
  status: 'resolved',
  priority: 'medium'
};

describe('TanStack Store - incidentStore', () => {
  beforeEach(() => {
    // Reset store to initial state
    incidentStore.setState(() => ({
      incidents: [],
      selectedIncident: undefined,
      currentUser: undefined,
      isLoading: false,
      error: undefined,
      filters: {
        statuses: [],
        priorities: [],
        categories: [],
        assigneeId: undefined,
        searchQuery: undefined,
        dateFrom: undefined,
        dateTo: undefined,
        labels: []
      },
      ui: {
        sidebarOpen: true,
        activeView: 'list',
        sort: { field: 'createdAt', direction: 'desc' },
        pagination: { page: 1, pageSize: 20, totalItems: 0, totalPages: 0 }
      }
    }));
  });

  describe('Store initialization', () => {
    it('should have correct initial state', () => {
      const state = incidentStore.state;
      expect(state.incidents).toEqual([]);
      expect(state.selectedIncident).toBeUndefined();
      expect(state.currentUser).toBeUndefined();
      expect(state.isLoading).toBe(false);
      expect(state.error).toBeUndefined();
      expect(state.ui.sidebarOpen).toBe(true);
      expect(state.ui.activeView).toBe('list');
    });

    it('should have correct filter initial state', () => {
      const { filters } = incidentStore.state;
      expect(filters.statuses).toEqual([]);
      expect(filters.priorities).toEqual([]);
      expect(filters.categories).toEqual([]);
      expect(filters.searchQuery).toBeUndefined();
    });
  });

  describe('Incident actions', () => {
    it('setIncidents should replace all incidents', () => {
      setIncidents([mockIncident, mockIncident2]);
      expect(incidentStore.state.incidents).toHaveLength(2);
      expect(incidentStore.state.isLoading).toBe(false);
    });

    it('addIncident should append to list', () => {
      addIncident(mockIncident);
      addIncident(mockIncident2);
      expect(incidentStore.state.incidents).toHaveLength(2);
    });

    it('updateIncident should update existing incident', () => {
      setIncidents([mockIncident]);
      const updated = { ...mockIncident, title: 'Updated Title' };
      updateIncident(updated);
      expect(incidentStore.state.incidents[0].title).toBe('Updated Title');
    });

    it('updateIncident should update selectedIncident if same id', () => {
      setIncidents([mockIncident]);
      selectIncident(mockIncident);
      const updated = { ...mockIncident, title: 'Updated Title' };
      updateIncident(updated);
      expect(incidentStore.state.selectedIncident?.title).toBe('Updated Title');
    });

    it('removeIncident should remove from list', () => {
      setIncidents([mockIncident, mockIncident2]);
      removeIncident(mockIncident.id);
      expect(incidentStore.state.incidents).toHaveLength(1);
      expect(incidentStore.state.incidents[0].id).toBe('inc-2');
    });

    it('removeIncident should clear selection if selected', () => {
      setIncidents([mockIncident]);
      selectIncident(mockIncident);
      removeIncident(mockIncident.id);
      expect(incidentStore.state.selectedIncident).toBeUndefined();
    });

    it('selectIncident should set selectedIncident', () => {
      selectIncident(mockIncident);
      expect(incidentStore.state.selectedIncident).toEqual(mockIncident);
    });
  });

  describe('User actions', () => {
    it('setCurrentUser should set user', () => {
      setCurrentUser(mockUser);
      expect(incidentStore.state.currentUser).toEqual(mockUser);
    });
  });

  describe('Loading/Error actions', () => {
    it('startLoading should set isLoading and clear error', () => {
      incidentStore.setState((s) => ({ ...s, error: 'Previous error' }));
      startLoading();
      expect(incidentStore.state.isLoading).toBe(true);
      expect(incidentStore.state.error).toBeUndefined();
    });

    it('setError should set error and stop loading', () => {
      startLoading();
      setError('Something went wrong');
      expect(incidentStore.state.error).toBe('Something went wrong');
      expect(incidentStore.state.isLoading).toBe(false);
    });

    it('clearError should clear error', () => {
      setError('Some error');
      clearError();
      expect(incidentStore.state.error).toBeUndefined();
    });
  });

  describe('Filter actions', () => {
    it('setStatusFilter should update status filter', () => {
      setStatusFilter(['open', 'inProgress']);
      expect(incidentStore.state.filters.statuses).toEqual(['open', 'inProgress']);
    });

    it('setPriorityFilter should update priority filter', () => {
      setPriorityFilter(['critical', 'high']);
      expect(incidentStore.state.filters.priorities).toEqual(['critical', 'high']);
    });

    it('setSearchQuery should update search query', () => {
      setSearchQuery('bug fix');
      expect(incidentStore.state.filters.searchQuery).toBe('bug fix');
    });

    it('clearFilters should reset all filters', () => {
      setStatusFilter(['open']);
      setPriorityFilter(['critical']);
      setSearchQuery('test');
      clearFilters();
      expect(incidentStore.state.filters.statuses).toEqual([]);
      expect(incidentStore.state.filters.priorities).toEqual([]);
      expect(incidentStore.state.filters.searchQuery).toBeUndefined();
    });
  });

  describe('UI actions', () => {
    it('toggleSidebar should toggle sidebarOpen', () => {
      expect(incidentStore.state.ui.sidebarOpen).toBe(true);
      toggleSidebar();
      expect(incidentStore.state.ui.sidebarOpen).toBe(false);
      toggleSidebar();
      expect(incidentStore.state.ui.sidebarOpen).toBe(true);
    });

    it('setActiveView should change view', () => {
      setActiveView('board');
      expect(incidentStore.state.ui.activeView).toBe('board');
    });

    it('setSort should update sort config', () => {
      setSort({ field: 'priority', direction: 'asc' });
      expect(incidentStore.state.ui.sort).toEqual({
        field: 'priority',
        direction: 'asc'
      });
    });

    it('setPage should update current page', () => {
      setPage(3);
      expect(incidentStore.state.ui.pagination.page).toBe(3);
    });
  });

  describe('Selectors', () => {
    beforeEach(() => {
      setIncidents([mockIncident, mockIncident2, mockIncident3]);
    });

    it('totalIncidents should return count', () => {
      expect(totalIncidents(incidentStore.state)).toBe(3);
    });

    it('openIncidents should return only open incidents', () => {
      const open = openIncidents(incidentStore.state);
      expect(open).toHaveLength(1);
      expect(open[0].status).toBe('open');
    });

    it('criticalIncidents should return only critical incidents', () => {
      const critical = criticalIncidents(incidentStore.state);
      expect(critical).toHaveLength(1);
      expect(critical[0].priority).toBe('critical');
    });

    it('incidentCountByStatus should return correct counts', () => {
      const counts = incidentCountByStatus(incidentStore.state);
      expect(counts.open).toBe(1);
      expect(counts.inProgress).toBe(1);
      expect(counts.resolved).toBe(1);
      expect(counts.closed).toBe(0);
    });

    it('hasActiveFilters should return false with no filters', () => {
      expect(hasActiveFilters(incidentStore.state)).toBe(false);
    });

    it('hasActiveFilters should return true with filters', () => {
      setStatusFilter(['open']);
      expect(hasActiveFilters(incidentStore.state)).toBe(true);
    });

    it('isSelectedIncidentResolved should work correctly', () => {
      selectIncident(mockIncident);
      expect(isSelectedIncidentResolved(incidentStore.state)).toBe(false);

      selectIncident(mockIncident3);
      expect(isSelectedIncidentResolved(incidentStore.state)).toBe(true);
    });

    it('filteredIncidents should filter by status', () => {
      setStatusFilter(['open']);
      const filtered = filteredIncidents(incidentStore.state);
      expect(filtered).toHaveLength(1);
      expect(filtered[0].status).toBe('open');
    });

    it('filteredIncidents should filter by priority', () => {
      setPriorityFilter(['critical']);
      const filtered = filteredIncidents(incidentStore.state);
      expect(filtered).toHaveLength(1);
      expect(filtered[0].priority).toBe('critical');
    });

    it('filteredIncidents should filter by search query', () => {
      setSearchQuery('Critical');
      const filtered = filteredIncidents(incidentStore.state);
      expect(filtered).toHaveLength(1);
      expect(filtered[0].title).toContain('Critical');
    });

    it('filteredIncidents should apply multiple filters', () => {
      setStatusFilter(['open', 'inProgress']);
      setPriorityFilter(['critical']);
      const filtered = filteredIncidents(incidentStore.state);
      expect(filtered).toHaveLength(1); // Only critical + open
    });
  });

  describe('Complex nested state updates', () => {
    it('should handle deeply nested updates correctly', () => {
      setIncidents([{
        ...mockIncident,
        sla: {
          policyId: 'sla-1',
          policyName: 'Standard',
          responseDueAt: '2024-01-16T10:00:00Z',
          resolutionDueAt: '2024-01-17T10:00:00Z',
          responseBreached: false,
          resolutionBreached: false,
          metrics: {
            responsePercentage: 95.5,
            resolutionPercentage: 88.0
          }
        }
      }]);

      const incident = incidentStore.state.incidents[0];
      expect(incident.sla?.policyName).toBe('Standard');
      expect(incident.sla?.metrics.responsePercentage).toBe(95.5);
    });

    it('should handle incidents with comments and attachments', () => {
      const incidentWithComments: Incident = {
        ...mockIncident,
        comments: [
          {
            id: 'comment-1',
            text: 'Looking into this',
            author: mockUser,
            createdAt: '2024-01-15T11:00:00Z',
            attachments: [
              {
                id: 'attach-1',
                fileName: 'screenshot.png',
                contentType: 'image/png',
                size: 1024,
                url: 'https://example.com/screenshot.png',
                uploadedAt: '2024-01-15T11:00:00Z'
              }
            ],
            reactions: [
              {
                userId: 'user-2',
                emoji: '👍',
                createdAt: '2024-01-15T11:05:00Z'
              }
            ]
          }
        ]
      };

      setIncidents([incidentWithComments]);
      const incident = incidentStore.state.incidents[0];

      expect(incident.comments).toHaveLength(1);
      expect(incident.comments[0].attachments).toHaveLength(1);
      expect(incident.comments[0].reactions).toHaveLength(1);
      expect(incident.comments[0].reactions[0].emoji).toBe('👍');
    });
  });

  describe('Store subscription', () => {
    it('should notify on state changes', () => {
      const values: number[] = [];
      const unsubscribe = incidentStore.subscribe(() => {
        values.push(incidentStore.state.incidents.length);
      });

      addIncident(mockIncident);
      addIncident(mockIncident2);
      removeIncident(mockIncident.id);

      expect(values).toEqual([1, 2, 1]);
      unsubscribe();
    });
  });
});
