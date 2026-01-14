import { createRouter, createRoute, createRootRoute, Outlet } from '@tanstack/react-router';

import { useRouter, useParams, useSearch, useNavigate, useMatch } from '@tanstack/react-router';

export interface ProductsSearch {
  category?: string;
  page?: number;
  sort?: string;
  q?: string;
}

export interface ProductParams {
  productId: string;
}

export const rootRoute = createRootRoute({ component: RootLayout, notFoundComponent: NotFound });

export const indexRoute = createRoute({ getParentRoute: () => rootRoute, path: '/', component: HomePage });

export const aboutRoute = createRoute({ getParentRoute: () => rootRoute, path: '/about', component: AboutPage });

export const productsRoute = createRoute({
getParentRoute: () => rootRoute,
path: '/products',
component: ProductsPage,
loader: async (ctx: unknown) => fetchProducts(ctx.search),
validateSearch: (search: unknown) => ({
category: search.category ?? undefined,
page: search.page ?? 1 as number | null,
sort: search.sort ?? 'newest' as string,
q: search.q
})
});

export const productRoute = createRoute({
getParentRoute: () => productsRoute,
path: '$productId',
component: ProductDetailPage,
loader: async (ctx: unknown) => fetchProduct(ctx.params.productId)
});

export const dashboardRoute = createRoute({
getParentRoute: () => rootRoute,
path: '/dashboard',
component: DashboardPage,
beforeLoad: async (ctx: unknown) => ({ user: await checkAuth() })
});

export const profileRoute = createRoute({ getParentRoute: () => dashboardRoute, path: '/profile', component: ProfilePage });

export const settingsRoute = createRoute({ getParentRoute: () => dashboardRoute, path: '/settings', component: SettingsPage });

export const routeTree = rootRoute.addChildren([indexRoute, aboutRoute, productsRoute.addChildren([productRoute]), dashboardRoute.addChildren([profileRoute, settingsRoute])]);

export const router = createRouter({ routeTree: routeTree, defaultPreload: 'intent', defaultPreloadDelay: 100 });

export type AppRouter = typeof router;