import React from 'react';
import { createRoot, hydrateRoot } from 'react-dom/client';
import { RouterProvider } from '@tanstack/react-router';
import { Provider } from '@react-spectrum/s2';
import '@react-spectrum/s2/page.css';
import { router } from './router';
import { getHydrationData } from './impulse-runtime';

// Get hydration data from SSR
const hydrationData = getHydrationData();

const appElement = document.getElementById('app')!;

const App = () => (
  <React.StrictMode>
    <Provider locale="en-US" colorScheme="light">
      <RouterProvider router={router} />
    </Provider>
  </React.StrictMode>
);

if (hydrationData) {
  // Hydrate the SSR-rendered content
  hydrateRoot(appElement, <App />);
} else {
  // Client-only render (dev mode or SPA navigation)
  createRoot(appElement).render(<App />);
}
