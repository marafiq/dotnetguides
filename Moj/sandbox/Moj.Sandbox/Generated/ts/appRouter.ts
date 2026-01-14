import { createRouter, createRoute, createRootRoute, Outlet } from '@tanstack/react-router';
import { useRouter, useParams, useSearch, useNavigate, useMatch } from '@tanstack/react-router';

//  Type Definitions

export interface ProductsSearch {
  category?: string;
  page?: number;
  sort?: string;
  q?: string;
}

export interface ProductParams {
  productId: string;
}

// Placeholder components (would be imported in real app)
declare const RootLayout: React.ComponentType;
declare const NotFound: React.ComponentType;
declare const HomePage: React.ComponentType;
declare const AboutPage: React.ComponentType;
declare const ProductsPage: React.ComponentType;
declare const ProductDetailPage: React.ComponentType;
declare const DashboardPage: React.ComponentType;
declare const ProfilePage: React.ComponentType;
declare const SettingsPage: React.ComponentType;

// Placeholder API functions
declare function fetchProducts(search: ProductsSearch): Promise<unknown>;
declare function fetchProduct(productId: string): Promise<unknown>;
declare function checkAuth(): Promise<{ user: unknown }>;

//  Route Definitions

export const rootRoute = createRootRoute({
  component: RootLayout,
  notFoundComponent: NotFound
});

export const indexRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: '/',
  component: HomePage
});

export const aboutRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: '/about',
  component: AboutPage
});

export const productsRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: '/products',
  component: ProductsPage,
  validateSearch: (search: Record<string, unknown>): ProductsSearch => ({
    category: search.category as string | undefined,
    page: (search.page as number | undefined) ?? 1,
    sort: (search.sort as string | undefined) ?? 'newest',
    q: search.q as string | undefined
  }),
  loader: async ({ search }) => {
    return fetchProducts(search);
  }
});

export const productRoute = createRoute({
  getParentRoute: () => productsRoute,
  path: '$productId',
  component: ProductDetailPage,
  loader: async ({ params }) => {
    return fetchProduct(params.productId);
  }
});

export const dashboardRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: '/dashboard',
  component: DashboardPage,
  beforeLoad: async () => {
    return {
      user: await checkAuth()
    };
  }
});

export const profileRoute = createRoute({
  getParentRoute: () => dashboardRoute,
  path: '/profile',
  component: ProfilePage
});

export const settingsRoute = createRoute({
  getParentRoute: () => dashboardRoute,
  path: '/settings',
  component: SettingsPage
});

//  Route Tree

export const routeTree = rootRoute.addChildren([
  indexRoute,
  aboutRoute,
  productsRoute.addChildren([
    productRoute
  ]),
  dashboardRoute.addChildren([
    profileRoute,
    settingsRoute
  ])
]);

//  Router Instance

export const router = createRouter({
  routeTree,
  defaultPreload: 'intent',
  defaultPreloadDelay: 100
});

export type AppRouter = typeof router;

// Register router for type safety
declare module '@tanstack/react-router' {
  interface Register {
    router: AppRouter;
  }
}
