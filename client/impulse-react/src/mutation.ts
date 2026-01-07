import { useState, useCallback } from 'react';
import type { Router } from '@tanstack/react-router';
import { ImpulseHeaders } from './types';

export type MutationState<TResponse> =
  | { status: 'idle' }
  | { status: 'pending' }
  | { status: 'success'; data: TResponse }
  | { status: 'error'; error: MutationError };

export interface MutationError {
  message: string;
  status: number;
  errors?: Record<string, string[]>;
}

export interface MutationOptions<TRequest, TResponse> {
  url: string;
  method?: 'POST' | 'PUT' | 'PATCH' | 'DELETE';
  invalidates?: string[];
  onSuccess?: (data: TResponse) => void;
  onError?: (error: MutationError) => void;
}

/**
 * Hook for executing mutations against Impulse endpoints.
 */
export function useMutation<TRequest, TResponse>(
  options: MutationOptions<TRequest, TResponse>,
  router?: Router<any, any, any>
): [
  MutationState<TResponse>,
  (data: TRequest) => Promise<TResponse | undefined>,
  () => void
] {
  const [state, setState] = useState<MutationState<TResponse>>({ status: 'idle' });

  const mutate = useCallback(
    async (data: TRequest): Promise<TResponse | undefined> => {
      setState({ status: 'pending' });

      try {
        const response = await fetch(options.url, {
          method: options.method ?? 'POST',
          headers: {
            'Content-Type': 'application/json',
            [ImpulseHeaders.Impulse]: 'true',
          },
          body: JSON.stringify(data),
        });

        if (!response.ok) {
          const errorBody = await response.json().catch(() => ({}));
          const error: MutationError = {
            message: errorBody.message ?? `Request failed with status ${response.status}`,
            status: response.status,
            errors: errorBody.errors,
          };
          setState({ status: 'error', error });
          options.onError?.(error);
          return undefined;
        }

        const result: TResponse = await response.json();
        setState({ status: 'success', data: result });
        options.onSuccess?.(result);

        // Invalidate routes if configured
        if (router && options.invalidates?.length) {
          for (const routeId of options.invalidates) {
            await router.invalidate({ routeId });
          }
        }

        return result;
      } catch (err) {
        const error: MutationError = {
          message: err instanceof Error ? err.message : 'Unknown error',
          status: 0,
        };
        setState({ status: 'error', error });
        options.onError?.(error);
        return undefined;
      }
    },
    [options, router]
  );

  const reset = useCallback(() => {
    setState({ status: 'idle' });
  }, []);

  return [state, mutate, reset];
}

/**
 * Creates a typed mutation hook for a specific endpoint.
 */
export function createMutation<TRequest, TResponse>(
  defaultOptions: Omit<MutationOptions<TRequest, TResponse>, 'onSuccess' | 'onError'>
) {
  return function useTypedMutation(
    overrides?: Partial<MutationOptions<TRequest, TResponse>>,
    router?: Router<any, any, any>
  ) {
    return useMutation<TRequest, TResponse>(
      { ...defaultOptions, ...overrides },
      router
    );
  };
}
