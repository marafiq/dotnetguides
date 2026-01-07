// ============================================================================
// Impulse Runtime - Entry Point
// ============================================================================

// Core runtime exports
export {
  // Types
  type ImpulsePayload,
  type AsyncState,
  type MutationState,
  type MutationOptions,
  type UseMutationResult,
  // Hooks
  useImpulseContext,
  useImpulseVersion,
  useDeferred,
  useLazy,
  useMutation,
  // Navigation
  navigate,
  // Helpers
  getPayloadFromDom,
  getComponentPathFromDom,
} from './runtime';

// App exports
export { registerComponent, getComponent, mount, renderPayload } from './App';

// ============================================================================
// Component Registration
// ============================================================================

import { registerComponent } from './App';

// Import feature components
import { Home } from '../Home/Component';

// Register components with namespace-derived paths
registerComponent('./Home', Home);
