import React from 'react';
import { createRoot, hydrateRoot } from 'react-dom/client';
import { RouterProvider } from '@tanstack/react-router';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { Provider } from '@react-spectrum/s2';
import '@react-spectrum/s2/page.css';
import { router } from './router';
import { getHydrationData } from './impulse-runtime';
import { ImpulseProvider } from './shared/ImpulseProvider';
import './styles.css';

// Create React Query client for mutations
const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      staleTime: 1000 * 60 * 5, // 5 minutes
    },
  },
});

// Get hydration data from SSR
const hydrationData = getHydrationData();

const appElement = document.getElementById('app')!;

const App = () => (
  <React.StrictMode>
    <QueryClientProvider client={queryClient}>
      <ImpulseProvider>
        <Provider locale="en-US" colorScheme="light">
          <RouterProvider router={router} />
        </Provider>
      </ImpulseProvider>
    </QueryClientProvider>
  </React.StrictMode>
);

if (hydrationData) {
  // Hydrate the SSR-rendered content
  hydrateRoot(appElement, <App />);
} else {
  // Client-only render (dev mode or SPA navigation)
  createRoot(appElement).render(<App />);
}
