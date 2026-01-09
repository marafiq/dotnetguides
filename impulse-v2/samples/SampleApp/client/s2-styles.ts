// Re-export S2 style macro from client folder for use in Features
// This wrapper allows the macro to resolve node_modules correctly
import { style } from '@react-spectrum/s2/style' with { type: 'macro' };
export { style };
