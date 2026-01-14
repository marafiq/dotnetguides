import { describe, it, expect } from 'vitest';
import {
  userStore,
  selectIsAdmin,
  type UserState,
  type UserPreferences
} from './userStore';
import {
  routeTree,
  router,
  type ProductsSearch
} from './appRouter';
import {
  queryClient,
  productsQueryOptions,
  productQueryOptions,
  type Product,
  type ProductsResponse
} from './productQueries';

describe('TanStack Store - userStore', () => {
  it('should have correct initial state shape', () => {
    const state = userStore.state;
    expect(state).toHaveProperty('name');
    expect(state).toHaveProperty('email');
    expect(state).toHaveProperty('isAuthenticated');
    expect(state).toHaveProperty('roles');
    expect(state).toHaveProperty('preferences');
  });

  it('should have correct initial values', () => {
    const state = userStore.state;
    expect(state.name).toBe('');
    expect(state.email).toBe('');
    expect(state.isAuthenticated).toBe(false);
    expect(state.roles).toEqual([]);
    expect(state.preferences.theme).toBe('light');
    expect(state.preferences.language).toBe('en');
    expect(state.preferences.notifications).toBe(true);
  });

  it('selectIsAdmin should work correctly', () => {
    expect(selectIsAdmin({ ...userStore.state, roles: [] })).toBe(false);
    expect(selectIsAdmin({ ...userStore.state, roles: ['admin'] })).toBe(true);
    expect(selectIsAdmin({ ...userStore.state, roles: ['user'] })).toBe(false);
  });
});

describe('TanStack Router - appRouter', () => {
  it('should have a valid route tree', () => {
    expect(routeTree).toBeDefined();
  });

  it('should have a valid router instance', () => {
    expect(router).toBeDefined();
    expect(router).toHaveProperty('navigate');
    expect(router).toHaveProperty('state');
  });

  it('ProductsSearch type should be correctly shaped', () => {
    const search: ProductsSearch = {
      category: 'electronics',
      page: 1,
      sort: 'price',
      q: 'laptop'
    };
    expect(search.category).toBe('electronics');
  });
});

describe('TanStack Query - productQueries', () => {
  it('should have a valid query client', () => {
    expect(queryClient).toBeDefined();
    expect(queryClient.getDefaultOptions()).toBeDefined();
  });

  it('productsQueryOptions should generate correct query key', () => {
    const options = productsQueryOptions({ category: 'test' });
    expect(options.queryKey).toContain('products');
  });

  it('productQueryOptions should generate correct query key', () => {
    const options = productQueryOptions('123');
    expect(options.queryKey).toEqual(['product', '123']);
  });

  it('Product type should be correctly shaped', () => {
    const product: Product = {
      id: '1',
      name: 'Test Product',
      description: 'A test product',
      price: 99.99,
      category: 'test',
      tags: ['test'],
      inStock: true
    };
    expect(product.id).toBe('1');
    expect(product.price).toBe(99.99);
  });

  it('ProductsResponse type should include pagination', () => {
    const response: ProductsResponse = {
      products: [],
      total: 0,
      page: 1,
      pageSize: 10,
      hasNextPage: false
    };
    expect(response.total).toBe(0);
    expect(response.hasNextPage).toBe(false);
  });
});

describe('Type Safety', () => {
  it('UserState should enforce required properties', () => {
    // This is compile-time validation
    const state: UserState = {
      name: 'John',
      email: 'john@example.com',
      isAuthenticated: true,
      roles: ['user'],
      preferences: {
        theme: 'dark',
        language: 'en',
        notifications: false
      }
    };
    expect(state.name).toBe('John');
  });

  it('UserPreferences should have correct structure', () => {
    const prefs: UserPreferences = {
      theme: 'dark',
      language: 'fr',
      notifications: true
    };
    expect(prefs.theme).toBe('dark');
  });
});
