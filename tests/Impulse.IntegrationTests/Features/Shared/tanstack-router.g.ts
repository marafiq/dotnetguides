// This file is auto-generated from Minimal API routes
// Do not edit manually - regenerate with: dotnet impulse codegen

import {
  createRouter,
  createRoute,
  createRootRoute,
  Outlet,
} from '@tanstack/react-router';
import { createElement } from 'react';

// Generated types from server
import type {
  DashboardProps,
  ResidentsListProps,
  ResidentDetailProps,
  MedicationsProps,
  WizardProps,
  DynamicFormProps,
  DeleteConfirmationProps,
  EditResidentModalProps,
  PaneContainerProps,
  FilterPaneProps,
  ActivityFeedPaneProps,
} from './types.g';

// Components from registry
import { IMPULSE_COMPONENTS } from './registry.g';

// Impulse protocol headers
const IMPULSE_HEADERS = {
  Impulse: 'X-Impulse',
  Version: 'X-Impulse-Version',
  Reload: 'X-Impulse-Reload',
};

const IMPULSE_VERSION = '1.0.0';

// Generic loader for Impulse routes
async function impulseLoader<T>(url: string): Promise<T> {
  const response = await fetch(url, {
    headers: {
      [IMPULSE_HEADERS.Impulse]: 'true',
      [IMPULSE_HEADERS.Version]: IMPULSE_VERSION,
      Accept: 'application/json',
    },
  });

  if (response.headers.get(IMPULSE_HEADERS.Reload) === 'true') {
    window.location.reload();
    return new Promise(() => {});
  }

  if (!response.ok) {
    throw new Error(`Failed to load ${url}: ${response.status}`);
  }

  const data = await response.json();
  return data.props;
}

// Root route
const rootRoute = createRootRoute({
  component: () => createElement(Outlet),
});

// URL builder for parameterized routes
function buildUrl(template: string, params: Record<string, string>): string {
  let url = template;
  for (const [key, value] of Object.entries(params)) {
    url = url.replace(`{${key}}`, value);
    url = url.replace(new RegExp(`\\{${key}:[^}]+\\}`, 'g'), value);
  }
  return url;
}

// Routes generated from Minimal API
const indexRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: '/',
  loader: async () => impulseLoader<DashboardProps>('/'),
  component: function Index() {
    const data = indexRoute.useLoaderData();
    const Component = IMPULSE_COMPONENTS.get('./Dashboard');
    if (!Component) return createElement('div', null, 'Component not found');
    return createElement(Component, data as object);
  },
});

const residentsRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: '/residents',
  loader: async () => impulseLoader<ResidentsListProps>('/residents'),
  component: function Residents() {
    const data = residentsRoute.useLoaderData();
    const Component = IMPULSE_COMPONENTS.get('./Residents/List');
    if (!Component) return createElement('div', null, 'Component not found');
    return createElement(Component, data as object);
  },
});

const residentDetailRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: '/residents/$id',
  loader: async ({ params }) => {
    const url = buildUrl('/residents/{id}', params);
    return impulseLoader<ResidentDetailProps>(url);
  },
  component: function ResidentDetail() {
    const data = residentDetailRoute.useLoaderData();
    const Component = IMPULSE_COMPONENTS.get('./Residents/Detail');
    if (!Component) return createElement('div', null, 'Component not found');
    return createElement(Component, data as object);
  },
});

const residentMedicationsRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: '/residents/$id/medications',
  loader: async ({ params }) => {
    const url = buildUrl('/residents/{id}/medications', params);
    return impulseLoader<MedicationsProps>(url);
  },
  component: function ResidentMedications() {
    const data = residentMedicationsRoute.useLoaderData();
    const Component = IMPULSE_COMPONENTS.get('./Residents/Medications');
    if (!Component) return createElement('div', null, 'Component not found');
    return createElement(Component, data as object);
  },
});

const wizardRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: '/wizard',
  loader: async () => impulseLoader<WizardProps>('/wizard'),
  component: function Wizard() {
    const data = wizardRoute.useLoaderData();
    const Component = IMPULSE_COMPONENTS.get('./Wizard');
    if (!Component) return createElement('div', null, 'Component not found');
    return createElement(Component, data as object);
  },
});

const wizardStepRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: '/wizard/step/$step',
  loader: async ({ params }) => {
    const url = buildUrl('/wizard/step/{step}', params);
    return impulseLoader<WizardProps>(url);
  },
  component: function WizardStep() {
    const data = wizardStepRoute.useLoaderData();
    const Component = IMPULSE_COMPONENTS.get('./Wizard');
    if (!Component) return createElement('div', null, 'Component not found');
    return createElement(Component, data as object);
  },
});

const formsInsuranceRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: '/forms/insurance',
  loader: async () => impulseLoader<DynamicFormProps>('/forms/insurance'),
  component: function FormsInsurance() {
    const data = formsInsuranceRoute.useLoaderData();
    const Component = IMPULSE_COMPONENTS.get('./DynamicForms');
    if (!Component) return createElement('div', null, 'Component not found');
    return createElement(Component, data as object);
  },
});

const residentDeleteRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: '/residents/$id/delete',
  loader: async ({ params }) => {
    const url = buildUrl('/residents/{id}/delete', params);
    return impulseLoader<DeleteConfirmationProps>(url);
  },
  component: function ResidentDelete() {
    const data = residentDeleteRoute.useLoaderData();
    const Component = IMPULSE_COMPONENTS.get('./Modal/DeleteConfirmation');
    if (!Component) return createElement('div', null, 'Component not found');
    return createElement(Component, data as object);
  },
});

const residentEditRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: '/residents/$id/edit',
  loader: async ({ params }) => {
    const url = buildUrl('/residents/{id}/edit', params);
    return impulseLoader<EditResidentModalProps>(url);
  },
  component: function ResidentEdit() {
    const data = residentEditRoute.useLoaderData();
    const Component = IMPULSE_COMPONENTS.get('./Modal/EditResidentModal');
    if (!Component) return createElement('div', null, 'Component not found');
    return createElement(Component, data as object);
  },
});

const paneResidentRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: '/pane/residents/$id',
  loader: async ({ params }) => {
    const url = buildUrl('/pane/residents/{id}', params);
    return impulseLoader<PaneContainerProps>(url);
  },
  component: function PaneResident() {
    const data = paneResidentRoute.useLoaderData();
    const Component = IMPULSE_COMPONENTS.get('./Pane/Container');
    if (!Component) return createElement('div', null, 'Component not found');
    return createElement(Component, data as object);
  },
});

const residentsFilterRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: '/residents/filter',
  loader: async () => impulseLoader<FilterPaneProps>('/residents/filter'),
  component: function ResidentsFilter() {
    const data = residentsFilterRoute.useLoaderData();
    const Component = IMPULSE_COMPONENTS.get('./Pane/FilterPane');
    if (!Component) return createElement('div', null, 'Component not found');
    return createElement(Component, data as object);
  },
});

const activityRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: '/activity',
  loader: async () => impulseLoader<ActivityFeedPaneProps>('/activity'),
  component: function Activity() {
    const data = activityRoute.useLoaderData();
    const Component = IMPULSE_COMPONENTS.get('./Pane/ActivityFeedPane');
    if (!Component) return createElement('div', null, 'Component not found');
    return createElement(Component, data as object);
  },
});

// Route tree
const routeTree = rootRoute.addChildren([
  indexRoute,
  residentsRoute,
  residentDetailRoute,
  residentMedicationsRoute,
  wizardRoute,
  wizardStepRoute,
  formsInsuranceRoute,
  residentDeleteRoute,
  residentEditRoute,
  paneResidentRoute,
  residentsFilterRoute,
  activityRoute,
]);

// Create the router instance
export const router = createRouter({
  routeTree,
  defaultPreload: 'intent',
});

// Type declarations for TanStack Router
declare module '@tanstack/react-router' {
  interface Register {
    router: typeof router;
  }
}

// Type-safe route paths
export const Routes = {
  index: () => '/' as const,
  residents: () => '/residents' as const,
  residentDetail: (id: number) => `/residents/${id}` as const,
  residentMedications: (id: number) => `/residents/${id}/medications` as const,
  wizard: () => '/wizard' as const,
  wizardStep: (step: number) => `/wizard/step/${step}` as const,
  formsInsurance: () => '/forms/insurance' as const,
  residentDelete: (id: number) => `/residents/${id}/delete` as const,
  residentEdit: (id: number) => `/residents/${id}/edit` as const,
  paneResident: (id: number) => `/pane/residents/${id}` as const,
  residentsFilter: () => '/residents/filter' as const,
  activity: () => '/activity' as const,
} as const;
