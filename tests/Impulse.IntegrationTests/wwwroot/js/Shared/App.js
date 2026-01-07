import { jsx as _jsx } from "react/jsx-runtime";
import { createRoot } from 'react-dom/client';
// Component registry - populated by feature modules
window.__IMPULSE_COMPONENTS__ = {};
export function registerComponent(path, component) {
    window.__IMPULSE_COMPONENTS__[path] = component;
}
export function mount() {
    const root = document.getElementById('app');
    if (!root)
        return;
    const payloadStr = root.dataset.impulse;
    if (!payloadStr)
        return;
    const payload = JSON.parse(payloadStr);
    const componentPath = root.dataset.component;
    if (!componentPath)
        return;
    const Component = window.__IMPULSE_COMPONENTS__[componentPath];
    if (!Component) {
        console.error(`Component not found: ${componentPath}`);
        return;
    }
    const props = payload.props;
    createRoot(root).render(_jsx(Component, { ...props }));
    // Handle deferred loading
    if (payload.deferred) {
        for (const [, url] of Object.entries(payload.deferred)) {
            loadDeferred(url);
        }
    }
}
async function loadDeferred(url) {
    const container = document.querySelector(`[data-impulse-deferred="${url}"]`);
    if (!container)
        return;
    try {
        const res = await fetch(url, { headers: { 'X-Impulse': 'true' } });
        const data = await res.json();
        const componentPath = container.dataset.component;
        if (componentPath && window.__IMPULSE_COMPONENTS__[componentPath]) {
            const Component = window.__IMPULSE_COMPONENTS__[componentPath];
            const props = data.props;
            createRoot(container).render(_jsx(Component, { ...props }));
        }
    }
    catch {
        container.textContent = 'Failed to load';
    }
}
// Auto-mount on DOM ready
if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', mount);
}
else {
    mount();
}
//# sourceMappingURL=App.js.map