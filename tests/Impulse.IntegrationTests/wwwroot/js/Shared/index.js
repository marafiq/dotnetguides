// ============================================================================
// Impulse Runtime - Entry Point
// ============================================================================
// Core runtime exports
export { 
// Hooks
useImpulseContext, useImpulseVersion, useDeferred, useLazy, useMutation, 
// Navigation
navigate, 
// Helpers
getPayloadFromDom, getComponentPathFromDom, } from './runtime';
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
// Register components with namespace-derived paths
registerComponent('./Dashboard', Dashboard);
registerComponent('./Residents/List', ResidentsList);
registerComponent('./Residents/Detail', ResidentDetail);
registerComponent('./Residents/Medications', Medications);
//# sourceMappingURL=index.js.map