/**
 * The payload structure embedded in the shell HTML and returned during navigation.
 */
export interface ImpulsePayload<TContext = unknown, TProps = unknown> {
  url: string;
  version: string;
  props: TProps;
  context: TContext;
  deferred?: Record<string, string>;
  lazy?: Record<string, string>;
}

/**
 * Navigation response from server.
 */
export interface ImpulseNavigationResponse<TContext = unknown, TProps = unknown> {
  props: TProps;
  context: TContext;
}

/**
 * Configuration options for Impulse.
 */
export interface ImpulseConfig {
  rootElementId?: string;
  onVersionMismatch?: () => void;
  onNavigationError?: (error: Error) => void;
}

/**
 * Impulse headers for navigation requests.
 */
export const ImpulseHeaders = {
  Impulse: 'X-Impulse',
  Version: 'X-Impulse-Version',
  Reload: 'X-Impulse-Reload',
} as const;

/**
 * State of deferred/lazy data loading.
 */
export type LoadingState<T> =
  | { status: 'idle' }
  | { status: 'loading' }
  | { status: 'success'; data: T }
  | { status: 'error'; error: Error };
