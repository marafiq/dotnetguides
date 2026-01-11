/**
 * Impulse runtime - handles server communication and page navigation.
 */

// Version for reload detection (set by server response)
let serverVersion: string | null = null;

/**
 * HTTP methods for mutations.
 */
export type HttpMethod = 'POST' | 'PUT' | 'PATCH' | 'DELETE';

/**
 * Result of an Impulse page fetch.
 */
export interface ImpulsePageResult<T> {
  props: T;
  component: string;
  version: string;
}

/**
 * Validation error response from the server.
 */
export interface ValidationError {
  errors: Record<string, string[]>;
}

/**
 * Check if response is a validation error.
 */
export function isValidationError(error: unknown): error is ValidationError {
  return (
    typeof error === 'object' &&
    error !== null &&
    'errors' in error &&
    typeof (error as ValidationError).errors === 'object'
  );
}

/**
 * Build headers for Impulse requests.
 */
function buildHeaders(): Headers {
  const headers = new Headers({
    'Content-Type': 'application/json',
    'X-Impulse': 'true',
  });

  if (serverVersion) {
    headers.set('X-Impulse-Version', serverVersion);
  }

  return headers;
}

/**
 * Fetch a page from the server (for GET endpoints).
 */
export async function impulseFetch<T>(
  url: string,
  signal?: AbortSignal
): Promise<ImpulsePageResult<T>> {
  const response = await fetch(url, {
    method: 'GET',
    headers: buildHeaders(),
    signal,
  });

  // Check for reload header
  if (response.headers.get('X-Impulse-Reload') === 'true') {
    window.location.reload();
    throw new Error('Page reload required');
  }

  if (!response.ok) {
    if (response.status === 404) {
      throw new Error('Page not found');
    }
    throw new Error(`Failed to fetch: ${response.status}`);
  }

  const result = await response.json() as ImpulsePageResult<T>;

  // Store server version for future requests
  if (result.version && !serverVersion) {
    serverVersion = result.version;
  }

  return result;
}

/**
 * Send a mutation to the server (for POST/PUT/PATCH/DELETE endpoints).
 */
export async function impulseMutate<TRequest, TResponse>(
  url: string,
  data: TRequest,
  method: HttpMethod = 'POST',
  signal?: AbortSignal
): Promise<TResponse> {
  // Replace route params in URL with values from data
  const resolvedUrl = resolveRouteParams(url, data as Record<string, unknown>);

  const response = await fetch(resolvedUrl, {
    method,
    headers: buildHeaders(),
    body: JSON.stringify(data),
    signal,
  });

  // Check for reload header
  if (response.headers.get('X-Impulse-Reload') === 'true') {
    window.location.reload();
    throw new Error('Page reload required');
  }

  if (!response.ok) {
    if (response.status === 422) {
      const error = await response.json();
      throw error;
    }
    if (response.status === 404) {
      throw new Error('Resource not found');
    }
    throw new Error(`Mutation failed: ${response.status}`);
  }

  return response.json() as Promise<TResponse>;
}

/**
 * Replace route params like :id or {id} with values from data.
 */
function resolveRouteParams(url: string, data: Record<string, unknown>): string {
  return url.replace(/:(\w+)/g, (_, key) => {
    const value = data[key];
    if (value === undefined || value === null) {
      throw new Error(`Missing route parameter: ${key}`);
    }
    return String(value);
  }).replace(/\{(\w+)\}/g, (_, key) => {
    const value = data[key];
    if (value === undefined || value === null) {
      throw new Error(`Missing route parameter: ${key}`);
    }
    return String(value);
  });
}

/**
 * Get initial page data from SSR.
 */
export function getInitialPageData<T>(): ImpulsePageResult<T> | null {
  if (typeof document === 'undefined') return null;

  const appElement = document.getElementById('app');
  if (!appElement) return null;

  const dataAttr = appElement.getAttribute('data-impulse');
  if (!dataAttr) return null;

  try {
    const data = JSON.parse(dataAttr) as ImpulsePageResult<T>;

    // Store server version
    if (data.version) {
      serverVersion = data.version;
    }

    return data;
  } catch {
    return null;
  }
}

/**
 * Get current server version.
 */
export function getServerVersion(): string | null {
  return serverVersion;
}
