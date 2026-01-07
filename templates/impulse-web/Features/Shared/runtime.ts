import { useState, useEffect, useCallback, useContext, createContext } from 'react';

// ============================================================================
// Impulse Runtime - React hooks for server-driven UI
// ============================================================================

// Types from server payload
export interface ImpulsePayload<TProps = unknown, TContext = unknown> {
  url: string;
  version: string;
  props: TProps;
  context: TContext;
  deferred?: Record<string, string>;
  lazy?: Record<string, string>;
}

// Async data states
export type AsyncState<T> =
  | { status: 'idle' }
  | { status: 'loading' }
  | { status: 'success'; data: T }
  | { status: 'error'; error: Error };

// Mutation states
export type MutationState<TResponse> =
  | { status: 'idle' }
  | { status: 'loading' }
  | { status: 'success'; data: TResponse }
  | { status: 'error'; error: Error; validationErrors?: Record<string, string[]> };

// ============================================================================
// Context - holds payload data for hooks
// ============================================================================

interface ImpulseContextValue {
  payload: ImpulsePayload | null;
  version: string;
}

const ImpulseContext = createContext<ImpulseContextValue>({
  payload: null,
  version: '',
});

export const ImpulseProvider = ImpulseContext.Provider;

/**
 * Access the application context from server payload
 * @example
 * const ctx = useImpulseContext<AppContext>();
 * console.log(ctx.user.name);
 */
export function useImpulseContext<TContext>(): TContext {
  const { payload } = useContext(ImpulseContext);
  if (!payload) {
    throw new Error('useImpulseContext must be used within ImpulseProvider');
  }
  return payload.context as TContext;
}

/**
 * Access the current payload version for navigation
 */
export function useImpulseVersion(): string {
  const { version } = useContext(ImpulseContext);
  return version;
}

// ============================================================================
// useDeferred - Auto-fetches data after initial render
// ============================================================================

const deferredCache = new Map<string, unknown>();
const deferredPromises = new Map<string, Promise<unknown>>();

/**
 * Auto-fetch deferred data after hydration
 * @param key - The deferred key from server payload
 * @param urls - Map of deferred URLs from payload
 * @example
 * const medications = useDeferred<MedicationsProps>('medications', deferredUrls);
 * if (medications.status === 'success') {
 *   return <MedicationList data={medications.data} />;
 * }
 */
export function useDeferred<T>(
  key: string,
  urls: Record<string, string>
): AsyncState<T> {
  const [state, setState] = useState<AsyncState<T>>(() => {
    // Check cache first
    if (deferredCache.has(key)) {
      return { status: 'success', data: deferredCache.get(key) as T };
    }
    return { status: 'idle' };
  });

  const url = urls[key];

  useEffect(() => {
    if (!url || state.status === 'success') return;

    // Check if already fetching
    if (deferredPromises.has(key)) {
      setState({ status: 'loading' });
      deferredPromises.get(key)!.then(
        (data) => setState({ status: 'success', data: data as T }),
        (error) => setState({ status: 'error', error })
      );
      return;
    }

    setState({ status: 'loading' });

    const promise = fetch(url, {
      headers: {
        'X-Impulse': 'true',
        'Accept': 'application/json',
      },
    })
      .then((res) => {
        if (!res.ok) throw new Error(`HTTP ${res.status}`);
        return res.json();
      })
      .then((json) => {
        const data = json.props ?? json;
        deferredCache.set(key, data);
        return data;
      });

    deferredPromises.set(key, promise);

    promise.then(
      (data) => setState({ status: 'success', data: data as T }),
      (error) => setState({ status: 'error', error })
    );

    return () => {
      deferredPromises.delete(key);
    };
  }, [key, url, state.status]);

  return state;
}

// ============================================================================
// useLazy - Fetch data on demand
// ============================================================================

/**
 * Lazy load data on demand
 * @param key - The lazy key from server payload
 * @param urls - Map of lazy URLs from payload
 * @returns [state, load] - Current state and function to trigger load
 * @example
 * const [documents, loadDocuments] = useLazy<DocumentsProps>('documents', lazyUrls);
 * return <button onClick={loadDocuments}>Load Documents</button>;
 */
export function useLazy<T>(
  key: string,
  urls: Record<string, string>
): [AsyncState<T>, () => void] {
  const [state, setState] = useState<AsyncState<T>>({ status: 'idle' });
  const url = urls[key];

  const load = useCallback(() => {
    if (!url || state.status === 'loading') return;

    setState({ status: 'loading' });

    fetch(url, {
      headers: {
        'X-Impulse': 'true',
        'Accept': 'application/json',
      },
    })
      .then((res) => {
        if (!res.ok) throw new Error(`HTTP ${res.status}`);
        return res.json();
      })
      .then((json) => {
        const data = json.props ?? json;
        setState({ status: 'success', data: data as T });
      })
      .catch((error) => {
        setState({ status: 'error', error });
      });
  }, [url, state.status]);

  return [state, load];
}

// ============================================================================
// useMutation - Execute mutations with optimistic updates
// ============================================================================

export interface MutationOptions<TRequest, TResponse> {
  /** Called before mutation starts */
  onMutate?: (request: TRequest) => void;
  /** Called on success */
  onSuccess?: (response: TResponse) => void;
  /** Called on error */
  onError?: (error: Error, validationErrors?: Record<string, string[]>) => void;
  /** Called after mutation completes (success or error) */
  onSettled?: () => void;
}

export interface UseMutationResult<TRequest, TResponse> {
  mutate: (request: TRequest) => Promise<TResponse>;
  state: MutationState<TResponse>;
  reset: () => void;
}

/**
 * Execute mutations to the server
 * @param url - The mutation endpoint URL
 * @param options - Callbacks for mutation lifecycle
 * @example
 * const { mutate, state } = useMutation<CreateResidentRequest, CreateResidentResponse>(
 *   '/api/residents',
 *   { onSuccess: (data) => navigate(`/residents/${data.id}`) }
 * );
 */
export function useMutation<TRequest, TResponse>(
  url: string,
  options: MutationOptions<TRequest, TResponse> = {}
): UseMutationResult<TRequest, TResponse> {
  const version = useImpulseVersion();
  const [state, setState] = useState<MutationState<TResponse>>({ status: 'idle' });

  const mutate = useCallback(
    async (request: TRequest): Promise<TResponse> => {
      options.onMutate?.(request);
      setState({ status: 'loading' });

      try {
        const res = await fetch(url, {
          method: 'POST',
          headers: {
            'Content-Type': 'application/json',
            'X-Impulse': 'true',
            'X-Impulse-Version': version,
          },
          body: JSON.stringify(request),
        });

        // Handle validation errors (422)
        if (res.status === 422) {
          const errorData = await res.json();
          const validationErrors = errorData.errors as Record<string, string[]>;
          const error = new Error('Validation failed');
          setState({ status: 'error', error, validationErrors });
          options.onError?.(error, validationErrors);
          options.onSettled?.();
          throw error;
        }

        if (!res.ok) {
          throw new Error(`HTTP ${res.status}`);
        }

        // Check for reload header (version mismatch)
        if (res.headers.get('X-Impulse-Reload') === 'true') {
          window.location.reload();
          throw new Error('Version mismatch - reloading');
        }

        const data = (await res.json()) as TResponse;
        setState({ status: 'success', data });
        options.onSuccess?.(data);
        options.onSettled?.();
        return data;
      } catch (error) {
        const err = error instanceof Error ? error : new Error(String(error));
        if (state.status !== 'error') {
          setState({ status: 'error', error: err });
          options.onError?.(err);
          options.onSettled?.();
        }
        throw err;
      }
    },
    [url, version, options, state.status]
  );

  const reset = useCallback(() => {
    setState({ status: 'idle' });
  }, []);

  return { mutate, state, reset };
}

// ============================================================================
// Navigation helpers
// ============================================================================

/**
 * Navigate to a new Impulse route
 * Sends X-Impulse headers for SPA navigation
 */
export async function navigate(url: string): Promise<ImpulsePayload> {
  const res = await fetch(url, {
    headers: {
      'X-Impulse': 'true',
      'Accept': 'application/json',
    },
  });

  if (!res.ok) {
    throw new Error(`Navigation failed: HTTP ${res.status}`);
  }

  // Check for reload header
  if (res.headers.get('X-Impulse-Reload') === 'true') {
    window.location.href = url;
    throw new Error('Version mismatch - reloading');
  }

  return res.json();
}

// ============================================================================
// SSR Hydration helpers
// ============================================================================

/**
 * Extract payload from server-rendered HTML
 */
export function getPayloadFromDom(): ImpulsePayload | null {
  const root = document.getElementById('app');
  if (!root) return null;

  const payloadStr = root.dataset.impulse;
  if (!payloadStr) return null;

  try {
    return JSON.parse(payloadStr);
  } catch {
    console.error('Failed to parse Impulse payload');
    return null;
  }
}

/**
 * Get component path from server-rendered HTML
 */
export function getComponentPathFromDom(): string | null {
  const root = document.getElementById('app');
  return root?.dataset.component ?? null;
}
