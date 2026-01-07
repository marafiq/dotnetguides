// Re-export all components for bundling
export { registerComponent, mount } from './App';
// Import and register all feature components
import { Dashboard } from '../Dashboard/Component';
import { ResidentsList } from '../Residents/List';
import { ResidentDetail, Medications } from '../Residents/Detail';
import { registerComponent } from './App';
registerComponent('./Dashboard', Dashboard);
registerComponent('./Residents/List', ResidentsList);
registerComponent('./Residents/Detail', ResidentDetail);
registerComponent('./Residents/Medications', Medications);
//# sourceMappingURL=index.js.map