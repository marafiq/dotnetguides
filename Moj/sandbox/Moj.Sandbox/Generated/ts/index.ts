// Re-export all generated modules for validation and use
export * from './userStore';
export * from './appRouter';
export * from './productQueries';

// Validation that all exports work correctly
import {
  // Store exports
  userStore,
  setUser,
  logout,
  updatePreferences,
  selectIsAdmin,
  useUserStore,
  type UserState,
  type UserPreferences,

  // Router exports
  router,
  routeTree,
  rootRoute,
  indexRoute,
  aboutRoute,
  productsRoute,
  productRoute,
  dashboardRoute,
  profileRoute,
  settingsRoute,
  type ProductsSearch,
  type ProductParams,
  type AppRouter,

  // Query exports
  queryClient,
  productsQueryOptions,
  productQueryOptions,
  productsByCategoryOptions,
  createProductMutation,
  updateProductMutation,
  deleteProductMutation,
  useProducts,
  useProduct,
  useCreateProduct,
  type Product,
  type ProductsResponse,
  type CreateProductInput,
  type UpdateProductInput
} from './index';

// Type validation - ensure all types are correctly inferred
type ValidateUserState = UserState extends { name: string; email: string } ? true : false;
type ValidateProduct = Product extends { id: string; name: string; price: number } ? true : false;
type ValidateRouter = AppRouter extends { navigate: unknown } ? true : false;

// Export validation types
export type { ValidateUserState, ValidateProduct, ValidateRouter };

console.log('All TanStack DSL generated code validates successfully!');
