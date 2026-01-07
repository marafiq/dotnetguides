import type { Router } from '@tanstack/react-router';
import { ImpulseHeaders, type ImpulseNavigationResponse } from './types';

/**
 * Creates a loader function for TanStack Router that fetches from Impulse endpoints.
 */
export function createImpulseLoader<TProps>(version: string) {
  return async ({ request }: { request: Request }): Promise<TProps> => {
    const url = new URL(request.url);

    const response = await fetch(url.pathname + url.search, {
      headers: {
        [ImpulseHeaders.Impulse]: 'true',
        [ImpulseHeaders.Version]: version,
        Accept: 'application/json',
      },
      signal: request.signal,
    });

    // Check for version mismatch
    if (response.headers.get(ImpulseHeaders.Reload) === 'true') {
      window.location.reload();
      // Return a promise that never resolves since we're reloading
      return new Promise(() => {});
    }

    if (!response.ok) {
      throw new Error(`Failed to load ${url.pathname}: ${response.status}`);
    }

    const data: ImpulseNavigationResponse<unknown, TProps> = await response.json();
    return data.props;
  };
}

/**
 * Configuration for Impulse router integration.
 */
export interface ImpulseRouterConfig {
  version: string;
  onContextUpdate?: (context: unknown) => void;
}

/**
 * Creates router context with Impulse integration.
 */
export function createImpulseRouterContext<TContext>(config: ImpulseRouterConfig) {
  return {
    impulse: {
      version: config.version,
      onContextUpdate: config.onContextUpdate,
    },
  };
}

/**
 * Invalidates routes after a mutation.
 * Call this after successful mutations to refresh affected data.
 */
export async function invalidateRoutes(
  router: Router<any, any, any>,
  filter?: { routeId?: string }
) {
  await router.invalidate(filter);
}

/**
 * Helper to create a typed route path function.
 */
export function createRoutePath<TParams extends Record<string, string | number>>(
  template: string
): (params: TParams) => string {
  return (params: TParams) => {
    let path = template;
    for (const [key, value] of Object.entries(params)) {
      path = path.replace(`{${key}}`, String(value));
      path = path.replace(`:${key}`, String(value));
    }
    return path;
  };
}
