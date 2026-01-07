// Core types
export type {
  ImpulsePayload,
  ImpulseNavigationResponse,
  ImpulseConfig,
  LoadingState,
} from './types';
export { ImpulseHeaders } from './types';

// Context and provider
export { ImpulseProvider, useImpulseContext, useImpulseState } from './context';

// Mount functions
export { mount, parsePayload } from './mount';

// Hooks for deferred and lazy loading
export { useDeferred, useLazy } from './hooks';

// Router integration
export {
  createImpulseLoader,
  createImpulseRouterContext,
  invalidateRoutes,
  createRoutePath,
  type ImpulseRouterConfig,
} from './router';

// Mutations
export {
  useMutation,
  createMutation,
  type MutationState,
  type MutationError,
  type MutationOptions,
} from './mutation';
