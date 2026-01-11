import type { ComponentRegistry } from '@impulse/react';
import { ResidentsList } from './ResidentsList';
import { AdmissionWizard } from './AdmissionWizard';

/**
 * Component registry - maps server component names to React components.
 * The server sends { component: "ResidentsList", props: {...} }
 * and ImpulseHost looks up the component here to render it.
 */
export const components: ComponentRegistry = {
  ResidentsList,
  AdmissionWizard,
};
