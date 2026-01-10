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
  GetDashboardResponse,
} from '../generated/types';
import { RoutePaths } from '../generated/routePaths';

// Import colocated components - the Impulse way
import { ResidentsList, ResidentDetail } from '../Features/Residents/Components';
import { CreateResidentPage } from '../Features/Residents/CreateForm';
import { MedicationsList, MedicationDetail } from '../Features/Medications/Components';
import { CarePlanDetail } from '../Features/CarePlans/Components';
import { Dashboard } from '../Features/Dashboard/Components';
import { AdmissionWizardPage } from '../Features/Admission/AdmissionWizard';

// Create Impulse context
const ctx = createImpulseContext();

// ========================================
// Root Layout - Server-Driven Navigation
// ========================================

const RootLayout = () => (
  <div className="app-layout">
    <nav className="app-nav">
      <Link to="/" className="nav-brand">
        Senior Living CRM
      </Link>
      <div className="nav-links">
        <Link to="/dashboard" className="nav-link">
          Dashboard
        </Link>
        <Link to="/residents" className="nav-link">
          Residents
        </Link>
        <Link to="/admission" className="nav-link">
          Admission
        </Link>
      </div>
    </nav>
    <main>
      <Outlet />
    </main>
    <footer className="app-footer">
      <p>Powered by Impulse v2 - Server-Driven UI</p>
    </footer>
  </div>
);

// ========================================
// Route Definitions
// Each route uses server-driven data loading
// ========================================

const rootRoute = createRootRoute({
  component: RootLayout,
});

// Home redirect to dashboard
const homeRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: '/',
  component: () => {
    // Redirect to dashboard - server drives the experience
    window.location.href = '/dashboard';
    return <div className="app-main"><p>Redirecting to Dashboard...</p></div>;
  },
});

// Dashboard route - Server aggregates all data
const dashboardRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: '/dashboard',
  loader: async () => {
    const hydration = getHydrationData<GetDashboardResponse>();
    if (hydration && window.location.pathname === '/dashboard') {
      return hydration.props;
    }
    return ctx.impulse<GetDashboardResponse>(RoutePaths.GetDashboard);
  },
  component: function DashboardPage() {
    const data = dashboardRoute.useLoaderData();
    return <Dashboard data={data} />;
  },
});

// Residents list route
const residentsRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: '/residents',
  loader: async () => {
    const hydration = getHydrationData<ListResidentsResponse>();
    if (hydration && window.location.pathname === '/residents') {
      return hydration.props;
    }
    return ctx.impulse<ListResidentsResponse>(RoutePaths.ListResidents);
  },
  component: function ResidentsPage() {
    const data = residentsRoute.useLoaderData();
    return <ResidentsList data={data} />;
  },
});

// Create resident route - demonstrates mutations
const createResidentRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: '/residents/new',
  component: CreateResidentPage,
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

// Admission Wizard route - Multi-step server-driven form
const admissionWizardRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: '/admission',
  component: AdmissionWizardPage,
});

// ========================================
// Route Tree & Router
// ========================================

const routeTree = rootRoute.addChildren([
  homeRoute,
  dashboardRoute,
  residentsRoute,
  createResidentRoute,
  residentDetailRoute,
  medicationsRoute,
  medicationDetailRoute,
  carePlanRoute,
  admissionWizardRoute,
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
