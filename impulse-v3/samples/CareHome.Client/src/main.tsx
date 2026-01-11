import { createRoot } from 'react-dom/client';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { ImpulseProvider, ImpulseHost } from '@impulse/react';
import { components } from './components';
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
      <div className="app">
        <header className="app-header">
          <h1>Green Valley Care Home</h1>
        </header>
        <main className="app-main">
          <ImpulseHost
            components={components}
            initialUrl="/"
            loadingComponent={<div className="loading">Loading...</div>}
            notFoundComponent={<div className="error">Page not found</div>}
          />
        </main>
      </div>
    </ImpulseProvider>
  </QueryClientProvider>
);
