import { createRoot } from 'react-dom/client';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { ImpulseProvider } from '@impulse/react';
import { App } from './App';
import './styles.css';

const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      staleTime: 0,
      refetchOnWindowFocus: true,
    },
  },
});

const root = createRoot(document.getElementById('app')!);

root.render(
  <QueryClientProvider client={queryClient}>
    <ImpulseProvider baseUrl="">
      <App />
    </ImpulseProvider>
  </QueryClientProvider>
);
