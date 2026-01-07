import { useState, useEffect, useCallback, useRef } from 'react';
import { useImpulseState } from './context';
import { ImpulseHeaders, type LoadingState } from './types';

/**
 * Hook for deferred data that auto-loads after hydration.
 * Deferred data is fetched automatically when the component mounts.
 */
export function useDeferred<T>(
  key: string,
  deferredUrls: Record<string, string> | undefined
): LoadingState<T> {
  const { version, config } = useImpulseState();
  const [state, setState] = useState<LoadingState<T>>({ status: 'idle' });
  const fetchedRef = useRef(false);

  const url = deferredUrls?.[key];

  useEffect(() => {
    if (!url || fetchedRef.current) return;

    fetchedRef.current = true;
    setState({ status: 'loading' });

    fetchImpulseData<T>(url, version)
      .then((data) => {
        setState({ status: 'success', data });
      })
      .catch((error) => {
        setState({ status: 'error', error });
        config.onNavigationError?.(error);
      });
  }, [url, version, config]);

  return state;
}

/**
 * Hook for lazy data that loads on demand.
 * Returns a load function that must be called to fetch the data.
 */
export function useLazy<T>(
  key: string,
  lazyUrls: Record<string, string> | undefined
): [LoadingState<T>, () => void] {
  const { version, config } = useImpulseState();
  const [state, setState] = useState<LoadingState<T>>({ status: 'idle' });

  const url = lazyUrls?.[key];

  const load = useCallback(() => {
    if (!url || state.status === 'loading') return;

    setState({ status: 'loading' });

    fetchImpulseData<T>(url, version)
      .then((data) => {
        setState({ status: 'success', data });
      })
      .catch((error) => {
        setState({ status: 'error', error });
        config.onNavigationError?.(error);
      });
  }, [url, version, state.status, config]);

  return [state, load];
}

/**
 * Hook that returns props from the current route's loader data.
 * This is the primary way components receive their props from the server.
 */
export function useProps<TProps>(): TProps {
  // This will be provided by TanStack Router's loader
  // The actual implementation depends on how routes are set up
  throw new Error(
    'useProps must be implemented with TanStack Router integration. ' +
    'Use Route.useLoaderData() from your route definition instead.'
  );
}

/**
 * Fetches data from an Impulse endpoint.
 */
async function fetchImpulseData<T>(url: string, version: string): Promise<T> {
  const response = await fetch(url, {
    headers: {
      [ImpulseHeaders.Impulse]: 'true',
      [ImpulseHeaders.Version]: version,
      'Accept': 'application/json',
    },
  });

  // Check for version mismatch
  if (response.headers.get(ImpulseHeaders.Reload) === 'true') {
    window.location.reload();
    // Return a promise that never resolves since we're reloading
    return new Promise(() => {});
  }

  if (!response.ok) {
    throw new Error(`Failed to fetch ${url}: ${response.status}`);
  }

  const data = await response.json();
  return data.props ?? data;
}
