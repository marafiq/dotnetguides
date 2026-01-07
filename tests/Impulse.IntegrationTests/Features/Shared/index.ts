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
import { Dashboard } from '../Dashboard/Component';
import { ResidentsList } from '../Residents/List';
import { ResidentDetail, Medications } from '../Residents/Detail';
import { Wizard } from '../Wizard/Component';
import { DynamicForm, InsuranceApplicationForm } from '../DynamicForms/Component';
import { ModalContainer, DeleteConfirmation, EditResidentModal } from '../Modal/Component';
import { PaneContainer, ResidentDetailPane, ActivityFeedPane, FilterPane } from '../Pane/Component';

// Register components with namespace-derived paths
registerComponent('./Dashboard', Dashboard);
registerComponent('./Residents/List', ResidentsList);
registerComponent('./Residents/Detail', ResidentDetail);
registerComponent('./Residents/Medications', Medications);
registerComponent('./Wizard', Wizard);
registerComponent('./DynamicForms', DynamicForm);
registerComponent('./DynamicForms/Insurance', InsuranceApplicationForm);
registerComponent('./Modal', ModalContainer);
registerComponent('./Modal/DeleteConfirmation', DeleteConfirmation);
registerComponent('./Modal/EditResident', EditResidentModal);
registerComponent('./Pane', PaneContainer);
registerComponent('./Pane/ResidentDetail', ResidentDetailPane);
registerComponent('./Pane/ActivityFeed', ActivityFeedPane);
registerComponent('./Pane/Filter', FilterPane);
