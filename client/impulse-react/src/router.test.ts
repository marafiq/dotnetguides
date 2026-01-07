import { describe, it, expect } from 'vitest';
import { createRoutePath, createImpulseRouterContext } from './router';

describe('createRoutePath', () => {
  it('creates path with no parameters', () => {
    const path = createRoutePath('/residents');
    expect(path({})).toBe('/residents');
  });

  it('replaces single parameter', () => {
    const path = createRoutePath<{ id: number }>('/residents/{id}');
    expect(path({ id: 42 })).toBe('/residents/42');
  });

  it('replaces multiple parameters', () => {
    const path = createRoutePath<{ residentId: number; medicationId: number }>(
      '/residents/{residentId}/medications/{medicationId}'
    );
    expect(path({ residentId: 1, medicationId: 5 })).toBe('/residents/1/medications/5');
  });

  it('handles colon-style parameters', () => {
    const path = createRoutePath<{ id: number }>('/residents/:id');
    expect(path({ id: 123 })).toBe('/residents/123');
  });

  it('handles string parameters', () => {
    const path = createRoutePath<{ slug: string }>('/posts/{slug}');
    expect(path({ slug: 'my-post' })).toBe('/posts/my-post');
  });
});

describe('createImpulseRouterContext', () => {
  it('creates context with version', () => {
    const context = createImpulseRouterContext({
      version: '1.0.0',
    });

    expect(context.impulse.version).toBe('1.0.0');
  });

  it('includes onContextUpdate callback', () => {
    const onContextUpdate = vi.fn();
    const context = createImpulseRouterContext({
      version: '1.0.0',
      onContextUpdate,
    });

    expect(context.impulse.onContextUpdate).toBe(onContextUpdate);
  });
});
