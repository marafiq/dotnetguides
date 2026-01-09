import React from 'react';
import {
  createRouter,
  createRootRoute,
  createRoute,
  Outlet,
  Link,
} from '@tanstack/react-router';
import { createImpulseContext, getHydrationData } from './impulse-runtime';

// Import generated types
import type {
  ListResidentsResponse,
  GetResidentResponse,
  ListMedicationsResponse,
  GetMedicationResponse,
  GetCarePlanResponse,
} from '../generated/types';
import { RoutePaths } from '../generated/routePaths';

// Import colocated components
import { ResidentsList, ResidentDetail } from '../Features/Residents/Components';
import { MedicationsList, MedicationDetail } from '../Features/Medications/Components';
import { CarePlanDetail } from '../Features/CarePlans/Components';

// Create Impulse context
const ctx = createImpulseContext();

// ========================================
// Root Layout
// ========================================

const RootLayout = () => (
  <div className="app-layout">
    <nav className="app-nav">
      <Link to="/" className="nav-brand">
        Senior Living CRM
      </Link>
      <div className="nav-links">
        <Link to="/residents" className="nav-link">
          Residents
        </Link>
      </div>
    </nav>
    <main className="app-main">
      <Outlet />
    </main>
    <footer className="app-footer">
      <p>Powered by Impulse v2</p>
    </footer>
  </div>
);

// ========================================
// Route Definitions
// ========================================

const rootRoute = createRootRoute({
  component: RootLayout,
});

// Home route
const homeRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: '/',
  component: () => (
    <div className="home-page">
      <h1>Welcome to Senior Living CRM</h1>
      <p>A demonstration of the Impulse v2 framework</p>
      <Link to="/residents" className="btn btn-primary">
        View Residents
      </Link>
    </div>
  ),
});

// Residents list route
const residentsRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: '/residents',
  loader: async () => {
    // Check for SSR hydration data first
    const hydration = getHydrationData<ListResidentsResponse>();
    if (hydration && window.location.pathname === '/residents') {
      return hydration.props;
    }
    // Otherwise fetch from server
    return ctx.impulse<ListResidentsResponse>(RoutePaths.ListResidents);
  },
  component: function ResidentsPage() {
    const data = residentsRoute.useLoaderData();
    return <ResidentsList data={data} />;
  },
});

// Resident detail route
const residentDetailRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: '/residents/$id',
  loader: async ({ params }) => {
    const hydration = getHydrationData<GetResidentResponse>();
    if (hydration && window.location.pathname === `/residents/${params.id}`) {
      return hydration.props;
    }
    const url = RoutePaths.GetResident.replace('{id}', params.id);
    return ctx.impulse<GetResidentResponse>(url);
  },
  component: function ResidentDetailPage() {
    const data = residentDetailRoute.useLoaderData();
    return <ResidentDetail data={data} />;
  },
});

// Medications list route
const medicationsRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: '/residents/$residentId/medications',
  loader: async ({ params }) => {
    const hydration = getHydrationData<ListMedicationsResponse>();
    if (hydration && window.location.pathname.includes('/medications')) {
      return hydration.props;
    }
    const url = RoutePaths.ListMedications.replace('{residentId}', params.residentId);
    return ctx.impulse<ListMedicationsResponse>(url);
  },
  component: function MedicationsPage() {
    const data = medicationsRoute.useLoaderData();
    return <MedicationsList data={data} />;
  },
});

// Medication detail route
const medicationDetailRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: '/residents/$residentId/medications/$medicationId',
  loader: async ({ params }) => {
    const hydration = getHydrationData<GetMedicationResponse>();
    if (hydration) {
      return hydration.props;
    }
    const url = RoutePaths.GetMedication
      .replace('{residentId}', params.residentId)
      .replace('{medicationId}', params.medicationId);
    return ctx.impulse<GetMedicationResponse>(url);
  },
  component: function MedicationDetailPage() {
    const data = medicationDetailRoute.useLoaderData();
    return <MedicationDetail data={data} />;
  },
});

// Care plan route
const carePlanRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: '/residents/$residentId/care-plan',
  loader: async ({ params }) => {
    const hydration = getHydrationData<GetCarePlanResponse>();
    if (hydration && window.location.pathname.includes('/care-plan')) {
      return hydration.props;
    }
    const url = RoutePaths.GetCarePlan.replace('{residentId}', params.residentId);
    return ctx.impulse<GetCarePlanResponse>(url);
  },
  component: function CarePlanPage() {
    const data = carePlanRoute.useLoaderData();
    return <CarePlanDetail data={data} />;
  },
});

// ========================================
// Route Tree & Router
// ========================================

const routeTree = rootRoute.addChildren([
  homeRoute,
  residentsRoute,
  residentDetailRoute,
  medicationsRoute,
  medicationDetailRoute,
  carePlanRoute,
]);

export const router = createRouter({
  routeTree,
  defaultPreload: 'intent',
});

// Type declaration for router
declare module '@tanstack/react-router' {
  interface Register {
    router: typeof router;
  }
}
