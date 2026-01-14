/**
 * Auto-generated TanStack Store from C# domain model by Moj.TanStack.DSL
 * Source: Moj.Sandbox.Stores.IncidentStore
 *
 * All types, actions, and selectors are inferred from C# code.
 */
import { Store } from '@tanstack/store';
import type {
  IncidentAppState,
  Incident,
  IncidentStatus,
  IncidentPriority,
  IncidentFilters,
  User,
  SortConfig,
  PaginationState
} from './types';

// ============= Initial State (from C# object) =============

const initialState: IncidentAppState = {
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
    sort: {
      field: 'createdAt',
      direction: 'desc'
    },
    pagination: {
      page: 1,
      pageSize: 20,
      totalItems: 0,
      totalPages: 0
    }
  }
};

// ============= Store Instance =============

export const incidentStore = new Store<IncidentAppState>(initialState);

// ============= Actions (from C# lambdas) =============

export const setIncidents = (incidents: Incident[]) => {
  incidentStore.setState((state) => ({
    ...state,
    incidents,
    isLoading: false
  }));
};

export const addIncident = (incident: Incident) => {
  incidentStore.setState((state) => ({
    ...state,
    incidents: [...state.incidents, incident]
  }));
};

export const updateIncident = (updated: Incident) => {
  incidentStore.setState((state) => ({
    ...state,
    incidents: state.incidents.map((i) =>
      i.id === updated.id ? updated : i
    ),
    selectedIncident: state.selectedIncident?.id === updated.id
      ? updated
      : state.selectedIncident
  }));
};

export const removeIncident = (id: string) => {
  incidentStore.setState((state) => ({
    ...state,
    incidents: state.incidents.filter((i) => i.id !== id),
    selectedIncident: state.selectedIncident?.id === id
      ? undefined
      : state.selectedIncident
  }));
};

export const selectIncident = (incident: Incident | undefined) => {
  incidentStore.setState((state) => ({
    ...state,
    selectedIncident: incident
  }));
};

export const setCurrentUser = (user: User) => {
  incidentStore.setState((state) => ({
    ...state,
    currentUser: user
  }));
};

export const startLoading = () => {
  incidentStore.setState((state) => ({
    ...state,
    isLoading: true,
    error: undefined
  }));
};

export const setError = (error: string) => {
  incidentStore.setState((state) => ({
    ...state,
    error,
    isLoading: false
  }));
};

export const clearError = () => {
  incidentStore.setState((state) => ({
    ...state,
    error: undefined
  }));
};

// Filter actions

export const setFilters = (filters: IncidentFilters) => {
  incidentStore.setState((state) => ({
    ...state,
    filters
  }));
};

export const setStatusFilter = (statuses: IncidentStatus[]) => {
  incidentStore.setState((state) => ({
    ...state,
    filters: { ...state.filters, statuses }
  }));
};

export const setPriorityFilter = (priorities: IncidentPriority[]) => {
  incidentStore.setState((state) => ({
    ...state,
    filters: { ...state.filters, priorities }
  }));
};

export const setSearchQuery = (query: string | undefined) => {
  incidentStore.setState((state) => ({
    ...state,
    filters: { ...state.filters, searchQuery: query }
  }));
};

export const clearFilters = () => {
  incidentStore.setState((state) => ({
    ...state,
    filters: {
      statuses: [],
      priorities: [],
      categories: [],
      assigneeId: undefined,
      searchQuery: undefined,
      dateFrom: undefined,
      dateTo: undefined,
      labels: []
    }
  }));
};

// UI actions

export const setSidebarOpen = (open: boolean) => {
  incidentStore.setState((state) => ({
    ...state,
    ui: { ...state.ui, sidebarOpen: open }
  }));
};

export const toggleSidebar = () => {
  incidentStore.setState((state) => ({
    ...state,
    ui: { ...state.ui, sidebarOpen: !state.ui.sidebarOpen }
  }));
};

export const setActiveView = (view: 'list' | 'board' | 'timeline') => {
  incidentStore.setState((state) => ({
    ...state,
    ui: { ...state.ui, activeView: view }
  }));
};

export const setSort = (sort: SortConfig) => {
  incidentStore.setState((state) => ({
    ...state,
    ui: { ...state.ui, sort }
  }));
};

export const setPage = (page: number) => {
  incidentStore.setState((state) => ({
    ...state,
    ui: {
      ...state.ui,
      pagination: { ...state.ui.pagination, page }
    }
  }));
};

export const setPagination = (pagination: PaginationState) => {
  incidentStore.setState((state) => ({
    ...state,
    ui: { ...state.ui, pagination }
  }));
};

// ============= Selectors (from C# expressions) =============

export const totalIncidents = (state: IncidentAppState): number => {
  return state.incidents.length;
};

export const openIncidents = (state: IncidentAppState): Incident[] => {
  return state.incidents.filter((i) => i.status === 'open');
};

export const criticalIncidents = (state: IncidentAppState): Incident[] => {
  return state.incidents.filter((i) => i.priority === 'critical');
};

export const assignedToCurrentUser = (state: IncidentAppState): Incident[] => {
  if (!state.currentUser) return [];
  return state.incidents.filter((i) =>
    i.assignedTo?.id === state.currentUser!.id
  );
};

export const filteredIncidents = (state: IncidentAppState): Incident[] => {
  let result = state.incidents;

  const { filters } = state;

  if (filters.statuses.length > 0) {
    result = result.filter((i) => filters.statuses.includes(i.status));
  }

  if (filters.priorities.length > 0) {
    result = result.filter((i) => filters.priorities.includes(i.priority));
  }

  if (filters.categories.length > 0) {
    result = result.filter((i) => filters.categories.includes(i.category));
  }

  if (filters.assigneeId) {
    result = result.filter((i) => i.assignedTo?.id === filters.assigneeId);
  }

  if (filters.searchQuery) {
    const search = filters.searchQuery.toLowerCase();
    result = result.filter((i) =>
      i.title.toLowerCase().includes(search) ||
      i.description.toLowerCase().includes(search)
    );
  }

  if (filters.labels.length > 0) {
    result = result.filter((i) =>
      filters.labels.some((l) => i.labels.includes(l))
    );
  }

  if (filters.dateFrom) {
    result = result.filter((i) =>
      i.metadata.createdAt >= filters.dateFrom!
    );
  }

  if (filters.dateTo) {
    result = result.filter((i) =>
      i.metadata.createdAt <= filters.dateTo!
    );
  }

  return result;
};

export const incidentCountByStatus = (
  state: IncidentAppState
): Record<IncidentStatus, number> => {
  const counts: Record<IncidentStatus, number> = {
    open: 0,
    inProgress: 0,
    onHold: 0,
    resolved: 0,
    closed: 0
  };

  for (const incident of state.incidents) {
    counts[incident.status]++;
  }

  return counts;
};

export const incidentCountByPriority = (
  state: IncidentAppState
): Record<IncidentPriority, number> => {
  const counts: Record<IncidentPriority, number> = {
    critical: 0,
    high: 0,
    medium: 0,
    low: 0
  };

  for (const incident of state.incidents) {
    counts[incident.priority]++;
  }

  return counts;
};

export const hasActiveFilters = (state: IncidentAppState): boolean => {
  const { filters } = state;
  return (
    filters.statuses.length > 0 ||
    filters.priorities.length > 0 ||
    filters.categories.length > 0 ||
    !!filters.searchQuery ||
    !!filters.assigneeId ||
    filters.labels.length > 0
  );
};

export const isSelectedIncidentResolved = (state: IncidentAppState): boolean => {
  return (
    state.selectedIncident?.status === 'resolved' ||
    state.selectedIncident?.status === 'closed'
  );
};

// ============= Subscription Helper =============

export const subscribeToStore = <T>(
  selector: (state: IncidentAppState) => T,
  callback: (value: T) => void
): (() => void) => {
  let previousValue = selector(incidentStore.state);
  callback(previousValue);

  return incidentStore.subscribe(() => {
    const newValue = selector(incidentStore.state);
    if (newValue !== previousValue) {
      previousValue = newValue;
      callback(newValue);
    }
  });
};
