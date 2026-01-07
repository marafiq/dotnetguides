import * as React from 'react';
import { createRoot } from 'react-dom/client';
import { ImpulseProvider, ImpulsePayload, getPayloadFromDom, getComponentPathFromDom } from './runtime';

// ============================================================================
// Component Registry
// ============================================================================

declare global {
  interface Window {
    __IMPULSE_COMPONENTS__: Map<string, React.ComponentType<unknown>>;
    __IMPULSE_VERSION__: string;
  }
}

window.__IMPULSE_COMPONENTS__ = new Map();
window.__IMPULSE_VERSION__ = '';

/**
 * Register a component for a given path
 * Path should match the namespace-derived path from server
 * @example
 * registerComponent('./Residents/Detail', ResidentDetail);
 */
export function registerComponent<TProps>(
  path: string,
  component: React.ComponentType<TProps>
): void {
  window.__IMPULSE_COMPONENTS__.set(path, component as React.ComponentType<unknown>);
}

/**
 * Get a registered component by path
 */
export function getComponent(path: string): React.ComponentType<unknown> | undefined {
  return window.__IMPULSE_COMPONENTS__.get(path);
}

// ============================================================================
// App Wrapper - Provides context to component tree
// ============================================================================

interface AppProps {
  payload: ImpulsePayload;
  Component: React.ComponentType<unknown>;
}

function App({ payload, Component }: AppProps): React.ReactElement {
  return (
    <ImpulseProvider value={{ payload, version: payload.version }}>
      <Component {...(payload.props as object)} />
    </ImpulseProvider>
  );
}

// ============================================================================
// Mount - Hydrate from server shell
// ============================================================================

let appRoot: ReturnType<typeof createRoot> | null = null;

/**
 * Mount the application from server-rendered shell
 * Reads payload from data-impulse attribute and renders component
 */
export function mount(): void {
  const rootElement = document.getElementById('app');
  if (!rootElement) {
    console.error('Impulse: #app element not found');
    return;
  }

  const payload = getPayloadFromDom();
  if (!payload) {
    console.error('Impulse: No payload found in data-impulse');
    return;
  }

  const componentPath = getComponentPathFromDom();
  if (!componentPath) {
    console.error('Impulse: No component path in data-component');
    return;
  }

  const Component = getComponent(componentPath);
  if (!Component) {
    console.error(`Impulse: Component not registered: ${componentPath}`);
    console.error('Registered components:', Array.from(window.__IMPULSE_COMPONENTS__.keys()));
    return;
  }

  // Store version for navigation
  window.__IMPULSE_VERSION__ = payload.version;

  // Create or reuse root
  if (!appRoot) {
    appRoot = createRoot(rootElement);
  }

  appRoot.render(<App payload={payload} Component={Component} />);
}

/**
 * Render a new payload (for SPA navigation)
 */
export function renderPayload(payload: ImpulsePayload, componentPath: string): void {
  const rootElement = document.getElementById('app');
  if (!rootElement) return;

  const Component = getComponent(componentPath);
  if (!Component) {
    console.error(`Impulse: Component not registered: ${componentPath}`);
    return;
  }

  window.__IMPULSE_VERSION__ = payload.version;

  if (!appRoot) {
    appRoot = createRoot(rootElement);
  }

  appRoot.render(<App payload={payload} Component={Component} />);
}

// ============================================================================
// Auto-mount on DOM ready
// ============================================================================

if (typeof document !== 'undefined') {
  if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', mount);
  } else {
    // DOM already loaded
    mount();
  }
}
