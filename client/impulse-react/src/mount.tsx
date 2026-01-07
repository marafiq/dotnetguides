import { createRoot, hydrateRoot } from 'react-dom/client';
import type { ReactNode } from 'react';
import { ImpulseProvider } from './context';
import type { ImpulsePayload, ImpulseConfig } from './types';

interface MountOptions<TContext> {
  /** Root element ID (default: 'app') */
  rootElementId?: string;
  /** Impulse configuration */
  config?: ImpulseConfig;
  /** Whether to hydrate (SSR) or render (CSR) */
  hydrate?: boolean;
}

/**
 * Mounts the Impulse application from the shell payload.
 *
 * @param App - The root React component
 * @param options - Mount options
 * @returns The initial payload for use in routing
 */
export function mount<TContext = unknown, TProps = unknown>(
  App: (props: { payload: ImpulsePayload<TContext, TProps> }) => ReactNode,
  options: MountOptions<TContext> = {}
): ImpulsePayload<TContext, TProps> | null {
  const { rootElementId = 'app', config = {}, hydrate = false } = options;

  const rootElement = document.getElementById(rootElementId);
  if (!rootElement) {
    console.error(`Impulse: Root element #${rootElementId} not found`);
    return null;
  }

  const payloadString = rootElement.dataset.impulse;
  if (!payloadString) {
    console.error('Impulse: No data-impulse attribute found on root element');
    return null;
  }

  let payload: ImpulsePayload<TContext, TProps>;
  try {
    payload = JSON.parse(payloadString);
  } catch (error) {
    console.error('Impulse: Failed to parse payload', error);
    return null;
  }

  const tree = (
    <ImpulseProvider initialPayload={payload} config={config}>
      <App payload={payload} />
    </ImpulseProvider>
  );

  if (hydrate) {
    hydrateRoot(rootElement, tree);
  } else {
    const root = createRoot(rootElement);
    root.render(tree);
  }

  return payload;
}

/**
 * Parses the Impulse payload from the DOM without mounting.
 * Useful for extracting the initial payload for router setup.
 */
export function parsePayload<TContext = unknown, TProps = unknown>(
  rootElementId = 'app'
): ImpulsePayload<TContext, TProps> | null {
  const rootElement = document.getElementById(rootElementId);
  if (!rootElement) {
    return null;
  }

  const payloadString = rootElement.dataset.impulse;
  if (!payloadString) {
    return null;
  }

  try {
    return JSON.parse(payloadString);
  } catch {
    return null;
  }
}
