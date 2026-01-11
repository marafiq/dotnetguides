import React, { useCallback, useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { useImpulse } from './ImpulseProvider';
import { getInitialPageData, isValidationError, type ValidationError } from './impulse-runtime';

/**
 * Component registry type - maps component names to React components.
 */
// eslint-disable-next-line @typescript-eslint/no-explicit-any
export type ComponentRegistry = Record<string, React.ComponentType<ImpulseComponentProps<any>>>;

/**
 * Props passed to every Impulse component.
 */
export interface ImpulseComponentProps<T = unknown> {
  /** The component's props from the server */
  data: T;
  /** Navigate to a new URL */
  navigate: (url: string) => void;
  /** Submit a form/mutation to the server */
  submit: <TReq, TRes>(url: string, data: TReq) => Promise<TRes>;
  /** Current validation errors */
  errors: Record<string, string[]>;
  /** Whether a mutation is in progress */
  isSubmitting: boolean;
}

/**
 * Props for ImpulseHost component.
 */
export interface ImpulseHostProps {
  /** Component registry mapping server component names to React components */
  components: ComponentRegistry;
  /** Initial URL to load (defaults to current pathname) */
  initialUrl?: string;
  /** Loading component */
  loadingComponent?: React.ReactNode;
  /** Error component */
  errorComponent?: (error: Error) => React.ReactNode;
  /** 404 component */
  notFoundComponent?: React.ReactNode;
}

/**
 * ImpulseHost - Server-driven UI host component.
 *
 * Fetches the current page from the server and dynamically renders
 * the component specified by the server with the provided props.
 */
export function ImpulseHost({
  components,
  initialUrl,
  loadingComponent = <div>Loading...</div>,
  errorComponent = (error) => <div>Error: {error.message}</div>,
  notFoundComponent = <div>Page not found</div>,
}: ImpulseHostProps) {
  const { impulseFetch, impulseMutate } = useImpulse();
  const queryClient = useQueryClient();

  // Current URL state (for client-side navigation)
  const [currentUrl, setCurrentUrl] = useState(
    initialUrl ?? (typeof window !== 'undefined' ? window.location.pathname : '/')
  );

  // Validation errors from last mutation
  const [validationErrors, setValidationErrors] = useState<Record<string, string[]>>({});

  // Fetch current page
  const { data: pageData, isLoading, error, isError } = useQuery({
    queryKey: ['impulse-page', currentUrl],
    queryFn: async ({ signal }) => {
      // Check for SSR data first
      const ssrData = getInitialPageData();
      if (ssrData && ssrData.component) {
        return ssrData;
      }
      return impulseFetch(currentUrl, signal);
    },
    staleTime: 0,
  });

  // Navigate to a new URL
  const navigate = useCallback((url: string) => {
    setValidationErrors({});
    // Update browser URL without full reload
    if (typeof window !== 'undefined') {
      window.history.pushState({}, '', url);
    }
    // Always invalidate and refetch, even for same URL (wizard step navigation)
    if (url === currentUrl) {
      queryClient.invalidateQueries({ queryKey: ['impulse-page', url] });
    } else {
      setCurrentUrl(url);
    }
  }, [currentUrl, queryClient]);

  // Submit mutation
  const mutation = useMutation({
    mutationFn: async ({ url, data }: { url: string; data: unknown }) => {
      return impulseMutate(url, data);
    },
    onSuccess: (result) => {
      setValidationErrors({});
      // If server returns a redirect, navigate to it
      if (result && typeof result === 'object' && 'redirect' in result) {
        navigate((result as { redirect: string }).redirect);
      } else {
        // Invalidate current page to refetch
        queryClient.invalidateQueries({ queryKey: ['impulse-page', currentUrl] });
      }
    },
    onError: (error) => {
      if (isValidationError(error)) {
        setValidationErrors((error as ValidationError).errors);
      }
    },
  });

  const submit = useCallback(async <TReq, TRes>(url: string, data: TReq): Promise<TRes> => {
    return mutation.mutateAsync({ url, data }) as Promise<TRes>;
  }, [mutation]);

  // Handle browser back/forward
  React.useEffect(() => {
    const handlePopState = () => {
      setCurrentUrl(window.location.pathname);
    };
    window.addEventListener('popstate', handlePopState);
    return () => window.removeEventListener('popstate', handlePopState);
  }, []);

  // Render states
  if (isLoading) {
    return <>{loadingComponent}</>;
  }

  if (isError) {
    if (error.message === 'Page not found') {
      return <>{notFoundComponent}</>;
    }
    return <>{errorComponent(error)}</>;
  }

  if (!pageData) {
    return <>{notFoundComponent}</>;
  }

  // Look up component in registry
  const Component = components[pageData.component];
  if (!Component) {
    console.error(`Component not found in registry: ${pageData.component}`);
    return <div>Unknown component: {pageData.component}</div>;
  }

  // Render component with Impulse props
  const impulseProps: ImpulseComponentProps = {
    data: pageData.props,
    navigate,
    submit,
    errors: validationErrors,
    isSubmitting: mutation.isPending,
  };

  return <Component {...impulseProps} />;
}
