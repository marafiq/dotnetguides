import { RouterProvider, createRouter, createRootRoute, createRoute, Outlet } from '@tanstack/react-router';
import type { ImpulsePayload } from '@impulse/react';
import { createImpulseLoader } from '@impulse/react';
import { Dashboard } from './components/Dashboard';
import { ResidentsList } from './components/ResidentsList';
import { ResidentDetail } from './components/ResidentDetail';
import { Layout } from './components/Layout';

// Define root route
const rootRoute = createRootRoute({
  component: Layout,
});

// Define routes
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
  component: ResidentDetail,
});

// Create router
const routeTree = rootRoute.addChildren([
  indexRoute,
  residentsRoute,
  residentDetailRoute,
]);

interface AppProps {
  payload: ImpulsePayload;
}

export function App({ payload }: AppProps) {
  const router = createRouter({
    routeTree,
    context: {
      impulseVersion: payload.version,
    },
  });

  return <RouterProvider router={router} />;
}
