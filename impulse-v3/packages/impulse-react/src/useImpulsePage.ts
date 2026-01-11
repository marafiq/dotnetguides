import { useQuery, type UseQueryResult } from '@tanstack/react-query';
import { useImpulse } from './ImpulseProvider';
import { getInitialPageData, type ImpulsePageResult } from './impulse-runtime';

/**
 * Options for useImpulsePage hook.
 */
export interface UseImpulsePageOptions {
  /**
   * Whether to use SSR data if available.
   * Default: true
   */
  useSSRData?: boolean;

  /**
   * Enable/disable the query.
   * Default: true
   */
  enabled?: boolean;

  /**
   * Stale time in milliseconds.
   * Default: 0 (always refetch on mount)
   */
  staleTime?: number;
}

/**
 * Hook to fetch a page from an Impulse endpoint.
 * Uses TanStack Query for caching and refetching.
 */
export function useImpulsePage<TProps>(
  url: string,
  options: UseImpulsePageOptions = {}
): UseQueryResult<TProps, Error> {
  const { impulseFetch } = useImpulse();
  const { useSSRData = true, enabled = true, staleTime = 0 } = options;

  // Check for SSR data on initial load
  const ssrData = useSSRData ? getInitialPageData<TProps>() : null;

  return useQuery<TProps, Error>({
    queryKey: ['impulse-page', url],
    queryFn: async ({ signal }) => {
      const result = await impulseFetch<TProps>(url, signal);
      return result.props;
    },
    enabled,
    staleTime,
    // Use SSR data as initial data if available and URL matches
    initialData: ssrData?.component === url ? ssrData.props : undefined,
  });
}

/**
 * Hook to fetch a page with route parameters.
 * Params are interpolated into the URL.
 */
export function useImpulsePageWithParams<TProps>(
  urlTemplate: string,
  params: Record<string, string | number>,
  options: UseImpulsePageOptions = {}
): UseQueryResult<TProps, Error> {
  // Interpolate params into URL
  const url = Object.entries(params).reduce(
    (acc, [key, value]) => acc.replace(`:${key}`, String(value)),
    urlTemplate
  );

  return useImpulsePage<TProps>(url, options);
}
