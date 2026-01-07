// ============================================================================
// Impulse Runtime - Entry Point
// ============================================================================

// Import styles
import './styles.css';

import * as React from 'react';
import { createRoot } from 'react-dom/client';
import { ImpulseProvider, ImpulsePayload, getPayloadFromDom, getComponentPathFromDom } from './runtime';

// Import auto-generated component registry
import { IMPULSE_COMPONENTS, getComponent } from './registry.g';

// Core runtime exports
export {
  // Types
  type ImpulsePayload,
  type AsyncState,
  type MutationState,
  type MutationOptions,
  type UseMutationResult,
  // Hooks
  useImpulseContext,
  useImpulseVersion,
  useDeferred,
  useLazy,
  useMutation,
  // Navigation
  navigate,
  // Helpers
  getPayloadFromDom,
  getComponentPathFromDom,
} from './runtime';

// Re-export registry
export { IMPULSE_COMPONENTS, getComponent } from './registry.g';

// Expose on window for debugging
if (typeof window !== 'undefined') {
  (window as unknown as { __IMPULSE_COMPONENTS__: Map<string, unknown> }).__IMPULSE_COMPONENTS__ = IMPULSE_COMPONENTS;
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
// Mount - Hydrate from server shell
// ============================================================================

let appRoot: ReturnType<typeof createRoot> | null = null;

/**
 * Mount the application from server-rendered shell
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
    console.error('Impulse: Component not registered:', componentPath);
    console.error('Registered components:', Array.from(IMPULSE_COMPONENTS.keys()));
    return;
  }

  // Create or reuse root
  if (!appRoot) {
    appRoot = createRoot(rootElement);
  }

  appRoot.render(React.createElement(App, { payload, Component }));
}

/**
 * Render a new payload (for SPA navigation)
 */
export function renderPayload(payload: ImpulsePayload, componentPath: string): void {
  const rootElement = document.getElementById('app');
  if (!rootElement) return;

  const Component = getComponent(componentPath);
  if (!Component) {
    console.error('Impulse: Component not registered:', componentPath);
    return;
  }

  if (!appRoot) {
    appRoot = createRoot(rootElement);
  }

  appRoot.render(React.createElement(App, { payload, Component }));
}

// ============================================================================
// Auto-mount on DOM ready
// ============================================================================

if (typeof document !== 'undefined') {
  if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', mount);
  } else {
    // DOM already loaded - mount immediately
    mount();
  }
}
