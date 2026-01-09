import React from 'react';
import { createRoot, hydrateRoot } from 'react-dom/client';
import { RouterProvider } from '@tanstack/react-router';
import { router } from './router';
import { getHydrationData } from './impulse-runtime';
import './styles.css';

// Get hydration data from SSR
const hydrationData = getHydrationData();

const appElement = document.getElementById('app')!;

if (hydrationData) {
  // Hydrate the SSR-rendered content
  hydrateRoot(
    appElement,
    <React.StrictMode>
      <RouterProvider router={router} />
    </React.StrictMode>
  );
} else {
  // Client-only render (dev mode or SPA navigation)
  createRoot(appElement).render(
    <React.StrictMode>
      <RouterProvider router={router} />
    </React.StrictMode>
  );
}
