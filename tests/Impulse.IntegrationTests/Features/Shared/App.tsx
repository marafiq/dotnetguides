import { createRoot } from 'react-dom/client';

// Impulse client runtime - mount from shell payload
interface ImpulsePayload<T = Record<string, unknown>> {
  url: string;
  version: string;
  props: T;
  context: unknown;
  deferred?: Record<string, string>;
}

declare global {
  interface Window {
    __IMPULSE_COMPONENTS__: Record<string, React.ComponentType<any>>;
  }
}

// Component registry - populated by feature modules
window.__IMPULSE_COMPONENTS__ = {};

export function registerComponent(path: string, component: React.ComponentType<any>) {
  window.__IMPULSE_COMPONENTS__[path] = component;
}

export function mount() {
  const root = document.getElementById('app');
  if (!root) return;

  const payloadStr = root.dataset.impulse;
  if (!payloadStr) return;

  const payload: ImpulsePayload = JSON.parse(payloadStr);
  const componentPath = root.dataset.component;

  if (!componentPath) return;

  const Component = window.__IMPULSE_COMPONENTS__[componentPath];
  if (!Component) {
    console.error(`Component not found: ${componentPath}`);
    return;
  }

  const props = payload.props as Record<string, unknown>;
  createRoot(root).render(<Component {...props} />);

  // Handle deferred loading
  if (payload.deferred) {
    for (const [, url] of Object.entries(payload.deferred)) {
      loadDeferred(url);
    }
  }
}

async function loadDeferred(url: string) {
  const container = document.querySelector(`[data-impulse-deferred="${url}"]`) as HTMLElement | null;
  if (!container) return;

  try {
    const res = await fetch(url, { headers: { 'X-Impulse': 'true' } });
    const data = await res.json();

    const componentPath = container.dataset.component;
    if (componentPath && window.__IMPULSE_COMPONENTS__[componentPath]) {
      const Component = window.__IMPULSE_COMPONENTS__[componentPath];
      const props = data.props as Record<string, unknown>;
      createRoot(container).render(<Component {...props} />);
    }
  } catch {
    container.textContent = 'Failed to load';
  }
}

// Auto-mount on DOM ready
if (document.readyState === 'loading') {
  document.addEventListener('DOMContentLoaded', mount);
} else {
  mount();
}
