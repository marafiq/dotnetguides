// ============================================================================
// Impulse Runtime - Hybrid Entry Point
// Supports both server-rendered hydration AND TanStack Router SPA mode
// ============================================================================

import './styles.css';

import * as React from 'react';
import { createRoot } from 'react-dom/client';

// Component registry
import { IMPULSE_COMPONENTS, getComponent } from './registry.g';

// Re-export
export { IMPULSE_COMPONENTS, getComponent };

// TanStack Router available for SPA mode (optional)
export { router, Routes } from './tanstack-router.g';

// Validation schemas (Zod) - mirrors server-side FluentValidation
export * from './validation.g';

// ============================================================================
// Types
// ============================================================================

interface ImpulsePayload {
  url: string;
  version: string;
  props: unknown;
  context: Record<string, unknown>;
  deferred?: Record<string, string>;
  lazy?: Record<string, string>;
}

// ============================================================================
// DOM Helpers
// ============================================================================

function getPayloadFromDom(): ImpulsePayload | null {
  const appElement = document.getElementById('app');
  if (!appElement) return null;

  const dataImpulse = appElement.getAttribute('data-impulse');
  if (!dataImpulse) return null;

  try {
    return JSON.parse(dataImpulse);
  } catch {
    console.error('Failed to parse data-impulse');
    return null;
  }
}

function getComponentPathFromDom(): string | null {
  const appElement = document.getElementById('app');
  return appElement?.getAttribute('data-component') ?? null;
}

// ============================================================================
// Context
// ============================================================================

interface ImpulseContextValue {
  payload: ImpulsePayload;
  version: string;
}

const ImpulseContext = React.createContext<ImpulseContextValue | null>(null);

function ImpulseProvider(props: React.PropsWithChildren<{ value: ImpulseContextValue }>) {
  return React.createElement(ImpulseContext.Provider, { value: props.value }, props.children);
}

// ============================================================================
// App Wrapper
// ============================================================================

interface AppProps {
  payload: ImpulsePayload;
  Component: React.ComponentType<unknown>;
}

function App({ payload, Component }: AppProps): React.ReactElement {
  return React.createElement(
    ImpulseProvider,
    { value: { payload, version: payload.version } },
    React.createElement(Component, payload.props as object)
  );
}

// ============================================================================
// Mount - Hydrate from server-rendered shell
// ============================================================================

let appRoot: ReturnType<typeof createRoot> | null = null;

export function mount(): void {
  const rootElement = document.getElementById('app');
  if (!rootElement) {
    console.error('Impulse: #app element not found');
    return;
  }

  const payload = getPayloadFromDom();
  const componentPath = getComponentPathFromDom();

  // If server provided props, hydrate immediately
  if (payload && componentPath) {
    const Component = getComponent(componentPath);
    if (!Component) {
      console.error('Impulse: Component not registered:', componentPath);
      console.error('Registered:', Array.from(IMPULSE_COMPONENTS.keys()));
      return;
    }

    if (!appRoot) {
      appRoot = createRoot(rootElement);
    }
    appRoot.render(React.createElement(App, { payload, Component }));
    return;
  }

  // No server props - this could be SPA mode
  // TanStack Router can be used here if needed
  console.log('Impulse: No server payload, SPA mode available via router export');
}

// ============================================================================
// Auto-mount
// ============================================================================

if (typeof document !== 'undefined') {
  if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', mount);
  } else {
    mount();
  }
}
