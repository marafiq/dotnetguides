import {
  createRouter,
  createRootRoute,
  createRoute,
  Outlet,
  Link,
} from '@tanstack/react-router';
import { Home, Users, UserPlus } from 'lucide-react';
import { Dashboard } from '../Features/Dashboard/Components';
import { ResidentsList, ResidentDetail } from '../Features/Residents/Components';
import { AdmissionWizardPage } from '../Features/Admission/AdmissionWizard';

// ========================================
// Root Layout with Tailwind
// ========================================

const rootRoute = createRootRoute({
  component: () => (
    <div className="min-h-screen flex flex-col">
      <nav className="bg-white border-b border-slate-200 sticky top-0 z-50">
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
          <div className="flex justify-between h-16">
            <div className="flex items-center">
              <h1 className="text-xl font-bold text-primary">
                Green Valley Care Home
              </h1>
            </div>
            <div className="flex items-center space-x-1">
              <NavLink to="/" icon={<Home className="h-4 w-4" />}>
                Dashboard
              </NavLink>
              <NavLink to="/residents" icon={<Users className="h-4 w-4" />}>
                Residents
              </NavLink>
              <NavLink to="/admission/wizard" icon={<UserPlus className="h-4 w-4" />} data-testid="nav-admission">
                New Admission
              </NavLink>
            </div>
          </div>
        </div>
      </nav>
      <main className="flex-1 py-8">
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
          <Outlet />
        </div>
      </main>
    </div>
  ),
});

function NavLink({ to, icon, children, ...props }: { to: string; icon: React.ReactNode; children: React.ReactNode; 'data-testid'?: string }) {
  return (
    <Link
      to={to}
      className="inline-flex items-center gap-2 px-3 py-2 text-sm font-medium text-slate-600 hover:text-primary hover:bg-slate-50 rounded-md transition-colors"
      activeProps={{ className: 'inline-flex items-center gap-2 px-3 py-2 text-sm font-medium text-primary bg-primary/5 rounded-md' }}
      {...props}
    >
      {icon}
      {children}
    </Link>
  );
}

// ========================================
// Routes
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

declare module '@tanstack/react-router' {
  interface Register {
    router: typeof router;
  }
}
