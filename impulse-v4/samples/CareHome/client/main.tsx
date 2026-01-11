import React from 'react';
import { createRoot, hydrateRoot } from 'react-dom/client';
import { RouterProvider } from '@tanstack/react-router';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { router } from './router';
import { getHydrationData } from './impulse-runtime';
import { ImpulseProvider } from './shared/ImpulseProvider';
import './styles.css';

const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      staleTime: 1000 * 60 * 5,
    },
  },
});

const hydrationData = getHydrationData();
const appElement = document.getElementById('app')!;

const App = () => (
  <React.StrictMode>
    <QueryClientProvider client={queryClient}>
      <ImpulseProvider>
        <RouterProvider router={router} />
      </ImpulseProvider>
    </QueryClientProvider>
  </React.StrictMode>
);

if (hydrationData) {
  hydrateRoot(appElement, <App />);
} else {
  createRoot(appElement).render(<App />);
}
