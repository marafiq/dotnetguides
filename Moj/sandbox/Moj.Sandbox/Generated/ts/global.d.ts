// Global type declarations for testing the generated code

import React from 'react';

declare global {
  // Placeholder React components for router testing
  const RootLayout: React.ComponentType<{ children?: React.ReactNode }>;
  const NotFound: React.ComponentType;
  const HomePage: React.ComponentType;
  const AboutPage: React.ComponentType;
  const ProductsPage: React.ComponentType;
  const ProductDetailPage: React.ComponentType;
  const DashboardPage: React.ComponentType;
  const ProfilePage: React.ComponentType;
  const SettingsPage: React.ComponentType;

  // API functions
  function fetchProducts(search: import('./appRouter').ProductsSearch): Promise<unknown>;
  function fetchProduct(productId: string): Promise<unknown>;
  function checkAuth(): Promise<{ user: unknown }>;
}

export {};
