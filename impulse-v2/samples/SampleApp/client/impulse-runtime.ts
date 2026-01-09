/**
 * Impulse React Runtime
 * Provides the client-side hydration and data fetching capabilities
 */

export interface ImpulseContext {
  /** Fetch data from an Impulse endpoint */
  impulse: <T>(url: string) => Promise<T>;
  /** Execute a mutation against an Impulse endpoint */
  impulseMutate: <TReq, TRes>(url: string, data: TReq, method: string) => Promise<TRes>;
}

export interface ImpulseData<T> {
  props: T;
  component: string;
  version: string;
}

/**
 * Create an Impulse context for data fetching
 */
export function createImpulseContext(): ImpulseContext {
  return {
    async impulse<T>(url: string): Promise<T> {
      const response = await fetch(url, {
        headers: {
          'X-Impulse': '1',
          'Accept': 'application/json',
        },
      });

      if (!response.ok) {
        throw new Error(`Failed to fetch ${url}: ${response.status}`);
      }

      const data: ImpulseData<T> = await response.json();

      // Check for version mismatch - reload if server version changed
      if (response.headers.get('X-Impulse-Reload') === 'true') {
        window.location.reload();
      }

      return data.props;
    },

    async impulseMutate<TReq, TRes>(
      url: string,
      data: TReq,
      method: string
    ): Promise<TRes> {
      const response = await fetch(url, {
        method,
        headers: {
          'Content-Type': 'application/json',
          'X-Impulse': '1',
        },
        body: JSON.stringify(data),
      });

      if (!response.ok) {
        const error = await response.json();
        throw new Error(error.error || `Mutation failed: ${response.status}`);
      }

      return response.json();
    },
  };
}

/**
 * Extract initial props from SSR hydration data
 */
export function getHydrationData<T>(): ImpulseData<T> | null {
  const appElement = document.getElementById('app');
  if (!appElement) return null;

  const dataAttr = appElement.getAttribute('data-impulse');
  if (!dataAttr) return null;

  try {
    return JSON.parse(dataAttr);
  } catch {
    console.error('Failed to parse Impulse hydration data');
    return null;
  }
}

/**
 * Hook for accessing Impulse context in components
 */
export function useImpulseContext(): ImpulseContext {
  // In a real implementation, this would use React context
  return createImpulseContext();
}
