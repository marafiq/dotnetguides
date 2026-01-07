// ============================================================================
// Impulse Runtime - Entry Point
// ============================================================================

// Import styles
import './styles.css';

import * as React from 'react';
import { createRoot } from 'react-dom/client';
import { ImpulseProvider, ImpulsePayload, getPayloadFromDom, getComponentPathFromDom } from './runtime';

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

// ============================================================================
// Component Registry (inline to prevent tree-shaking)
// ============================================================================

const IMPULSE_COMPONENTS = new Map<string, React.ComponentType<unknown>>();

/**
 * Register a component for a given path
 */
export function registerComponent<TProps>(
  path: string,
  component: React.ComponentType<TProps>
): void {
  IMPULSE_COMPONENTS.set(path, component as React.ComponentType<unknown>);
}

/**
 * Get a registered component by path
 */
export function getComponent(path: string): React.ComponentType<unknown> | undefined {
  return IMPULSE_COMPONENTS.get(path);
}

// ============================================================================
// Import and Register All Feature Components
// ============================================================================

import { Dashboard } from '../Dashboard/Component';
import { ResidentsList } from '../Residents/List';
import { ResidentDetail, Medications } from '../Residents/Detail';
import { Wizard } from '../Wizard/Component';
import { DynamicForm, InsuranceApplicationForm } from '../DynamicForms/Component';
import { ModalContainer, DeleteConfirmation, EditResidentModal } from '../Modal/Component';
import { PaneContainer, ResidentDetailPane, ActivityFeedPane, FilterPane } from '../Pane/Component';

// Register components with namespace-derived paths
// This MUST happen before mount()
// eslint-disable-next-line @typescript-eslint/no-explicit-any
const reg = (path: string, comp: React.ComponentType<any>) => {
  IMPULSE_COMPONENTS.set(path, comp as React.ComponentType<unknown>);
};

reg('./Dashboard', Dashboard);
reg('./Residents/List', ResidentsList);
reg('./Residents/Detail', ResidentDetail);
reg('./Residents/Medications', Medications);
reg('./Wizard', Wizard);
reg('./DynamicForms', DynamicForm);
reg('./DynamicForms/Insurance', InsuranceApplicationForm);
reg('./Modal', ModalContainer);
reg('./Modal/DeleteConfirmation', DeleteConfirmation);
reg('./Modal/EditResident', EditResidentModal);
reg('./Pane', PaneContainer);
reg('./Pane/ResidentDetail', ResidentDetailPane);
reg('./Pane/ActivityFeed', ActivityFeedPane);
reg('./Pane/Filter', FilterPane);

// Also expose on window for debugging
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

  const Component = IMPULSE_COMPONENTS.get(componentPath);
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

  const Component = IMPULSE_COMPONENTS.get(componentPath);
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
