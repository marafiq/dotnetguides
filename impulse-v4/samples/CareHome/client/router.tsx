import {
  createRouter,
  createRootRoute,
  createRoute,
  Outlet,
} from '@tanstack/react-router';
import { Dashboard } from '../Features/Dashboard/Components';
import { ResidentsList, ResidentDetail } from '../Features/Residents/Components';
import { AdmissionWizardPage } from '../Features/Admission/AdmissionWizard';

// ========================================
// Root Layout
// ========================================

const rootRoute = createRootRoute({
  component: () => (
    <div className="app-layout">
      <nav className="app-nav">
        <div className="nav-brand">
          <h1>Green Valley Care Home</h1>
        </div>
        <div className="nav-links">
          <a href="/">Dashboard</a>
          <a href="/residents">Residents</a>
          <a href="/admission/wizard" data-testid="nav-admission">New Admission</a>
        </div>
      </nav>
      <main className="app-main">
        <Outlet />
      </main>
    </div>
  ),
});

// ========================================
// Routes - Map to Feature Components
// ========================================

const indexRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: '/',
  component: Dashboard,
});

const residentsRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: '/residents',
  component: ResidentsList,
});

const residentDetailRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: '/residents/$id',
  component: function ResidentDetailRoute() {
    const { id } = residentDetailRoute.useParams();
    return <ResidentDetail id={Number(id)} />;
  },
});

const admissionWizardRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: '/admission/wizard',
  component: AdmissionWizardPage,
});

// ========================================
// Router Tree
// ========================================

const routeTree = rootRoute.addChildren([
  indexRoute,
  residentsRoute,
  residentDetailRoute,
  admissionWizardRoute,
]);

export const router = createRouter({ routeTree });

// Type declarations for type safety
declare module '@tanstack/react-router' {
  interface Register {
    router: typeof router;
  }
}
