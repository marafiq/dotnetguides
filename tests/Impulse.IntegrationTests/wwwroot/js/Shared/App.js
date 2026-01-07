import { jsx as _jsx } from "react/jsx-runtime";
import { createRoot } from 'react-dom/client';
import { ImpulseProvider, getPayloadFromDom, getComponentPathFromDom } from './runtime';
window.__IMPULSE_COMPONENTS__ = new Map();
window.__IMPULSE_VERSION__ = '';
/**
 * Register a component for a given path
 * Path should match the namespace-derived path from server
 * @example
 * registerComponent('./Residents/Detail', ResidentDetail);
 */
export function registerComponent(path, component) {
    window.__IMPULSE_COMPONENTS__.set(path, component);
}
/**
 * Get a registered component by path
 */
export function getComponent(path) {
    return window.__IMPULSE_COMPONENTS__.get(path);
}
function App({ payload, Component }) {
    return (_jsx(ImpulseProvider, { value: { payload, version: payload.version }, children: _jsx(Component, { ...payload.props }) }));
}
// ============================================================================
// Mount - Hydrate from server shell
// ============================================================================
let appRoot = null;
/**
 * Mount the application from server-rendered shell
 * Reads payload from data-impulse attribute and renders component
 */
export function mount() {
    const rootElement = document.getElementById('app');
    if (!rootElement) {
        console.error('Impulse: #app element not found');
        return;
    }
    const payload = getPayloadFromDom();
    if (!payload) {
        console.error('Impulse: No payload found in data-impulse');
        return;
    }
    const componentPath = getComponentPathFromDom();
    if (!componentPath) {
        console.error('Impulse: No component path in data-component');
        return;
    }
    const Component = getComponent(componentPath);
    if (!Component) {
        console.error(`Impulse: Component not registered: ${componentPath}`);
        console.error('Registered components:', Array.from(window.__IMPULSE_COMPONENTS__.keys()));
        return;
    }
    // Store version for navigation
    window.__IMPULSE_VERSION__ = payload.version;
    // Create or reuse root
    if (!appRoot) {
        appRoot = createRoot(rootElement);
    }
    appRoot.render(_jsx(App, { payload: payload, Component: Component }));
}
/**
 * Render a new payload (for SPA navigation)
 */
export function renderPayload(payload, componentPath) {
    const rootElement = document.getElementById('app');
    if (!rootElement)
        return;
    const Component = getComponent(componentPath);
    if (!Component) {
        console.error(`Impulse: Component not registered: ${componentPath}`);
        return;
    }
    window.__IMPULSE_VERSION__ = payload.version;
    if (!appRoot) {
        appRoot = createRoot(rootElement);
    }
    appRoot.render(_jsx(App, { payload: payload, Component: Component }));
}
// ============================================================================
// Auto-mount on DOM ready
// ============================================================================
if (typeof document !== 'undefined') {
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', mount);
    }
    else {
        // DOM already loaded
        mount();
    }
}
//# sourceMappingURL=App.js.map