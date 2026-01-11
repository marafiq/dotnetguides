import React, { createContext, useContext, useMemo } from 'react';
import {
  impulseFetch,
  impulseMutate,
  type HttpMethod,
  type ImpulsePageResult,
} from './impulse-runtime';

/**
 * Context value for Impulse.
 */
export interface ImpulseContext {
  /**
   * Fetch a page from the server.
   */
  impulseFetch: <T>(url: string, signal?: AbortSignal) => Promise<ImpulsePageResult<T>>;

  /**
   * Send a mutation to the server.
   */
  impulseMutate: <TRequest, TResponse>(
    url: string,
    data: TRequest,
    method?: HttpMethod,
    signal?: AbortSignal
  ) => Promise<TResponse>;

  /**
   * Base URL for API requests.
   */
  baseUrl: string;
}

const ImpulseContextValue = createContext<ImpulseContext | null>(null);

/**
 * Props for ImpulseProvider.
 */
export interface ImpulseProviderProps {
  /**
   * Base URL for API requests (default: empty string for same origin).
   */
  baseUrl?: string;

  /**
   * Children to render.
   */
  children: React.ReactNode;
}

/**
 * Provider component for Impulse context.
 */
export function ImpulseProvider({ baseUrl = '', children }: ImpulseProviderProps) {
  const contextValue = useMemo<ImpulseContext>(
    () => ({
      impulseFetch: async <T,>(url: string, signal?: AbortSignal) => {
        const fullUrl = baseUrl + url;
        return impulseFetch<T>(fullUrl, signal);
      },
      impulseMutate: async <TRequest, TResponse>(
        url: string,
        data: TRequest,
        method: HttpMethod = 'POST',
        signal?: AbortSignal
      ) => {
        const fullUrl = baseUrl + url;
        return impulseMutate<TRequest, TResponse>(fullUrl, data, method, signal);
      },
      baseUrl,
    }),
    [baseUrl]
  );

  return (
    <ImpulseContextValue.Provider value={contextValue}>
      {children}
    </ImpulseContextValue.Provider>
  );
}

/**
 * Hook to access Impulse context.
 * @throws If used outside of ImpulseProvider.
 */
export function useImpulse(): ImpulseContext {
  const context = useContext(ImpulseContextValue);
  if (!context) {
    throw new Error('useImpulse must be used within an ImpulseProvider');
  }
  return context;
}
