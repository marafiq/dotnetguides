// Impulse entry point - registers all components
export { registerComponent, mount } from './App';

// Import feature components
import { Home } from '../Home/Component';
import { registerComponent } from './App';

// Register components with their paths
registerComponent('./Home', Home);
