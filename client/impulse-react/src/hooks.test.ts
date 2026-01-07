import { describe, it, expect, vi, beforeEach } from 'vitest';
import { renderHook, waitFor } from '@testing-library/react';
import { useDeferred, useLazy } from './hooks';
import { ImpulseProvider } from './context';
import type { ImpulsePayload } from './types';
import type { ReactNode } from 'react';
import { createElement } from 'react';

describe('useDeferred', () => {
  const mockPayload: ImpulsePayload = {
    url: '/',
    version: '1.0.0',
    props: {},
    context: {},
  };

  const createWrapper = () => {
    return ({ children }: { children: ReactNode }) =>
      createElement(ImpulseProvider, { initialPayload: mockPayload }, children);
  };

  beforeEach(() => {
    vi.resetAllMocks();
  });

  it('returns idle state when no URL provided', () => {
    const { result } = renderHook(() => useDeferred('test', undefined), {
      wrapper: createWrapper(),
    });

    expect(result.current.status).toBe('idle');
  });

  it('returns idle state when key not found', () => {
    const { result } = renderHook(() => useDeferred('missing', { other: '/url' }), {
      wrapper: createWrapper(),
    });

    expect(result.current.status).toBe('idle');
  });

  it('fetches data automatically when URL is provided', async () => {
    const mockData = { props: { items: [1, 2, 3] } };
    (global.fetch as ReturnType<typeof vi.fn>).mockResolvedValueOnce({
      ok: true,
      headers: new Headers(),
      json: () => Promise.resolve(mockData),
    });

    const deferredUrls = { items: '/api/items' };
    const { result } = renderHook(() => useDeferred<{ items: number[] }>('items', deferredUrls), {
      wrapper: createWrapper(),
    });

    expect(result.current.status).toBe('loading');

    await waitFor(() => {
      expect(result.current.status).toBe('success');
    });

    if (result.current.status === 'success') {
      expect(result.current.data).toEqual({ items: [1, 2, 3] });
    }
  });

  it('handles fetch errors', async () => {
    (global.fetch as ReturnType<typeof vi.fn>).mockResolvedValueOnce({
      ok: false,
      status: 500,
      headers: new Headers(),
    });

    const deferredUrls = { items: '/api/items' };
    const { result } = renderHook(() => useDeferred('items', deferredUrls), {
      wrapper: createWrapper(),
    });

    await waitFor(() => {
      expect(result.current.status).toBe('error');
    });
  });
});

describe('useLazy', () => {
  const mockPayload: ImpulsePayload = {
    url: '/',
    version: '1.0.0',
    props: {},
    context: {},
  };

  const createWrapper = () => {
    return ({ children }: { children: ReactNode }) =>
      createElement(ImpulseProvider, { initialPayload: mockPayload }, children);
  };

  beforeEach(() => {
    vi.resetAllMocks();
  });

  it('returns idle state initially', () => {
    const lazyUrls = { docs: '/api/docs' };
    const { result } = renderHook(() => useLazy('docs', lazyUrls), {
      wrapper: createWrapper(),
    });

    const [state] = result.current;
    expect(state.status).toBe('idle');
  });

  it('does not fetch until load is called', () => {
    const lazyUrls = { docs: '/api/docs' };
    renderHook(() => useLazy('docs', lazyUrls), {
      wrapper: createWrapper(),
    });

    expect(global.fetch).not.toHaveBeenCalled();
  });

  it('fetches data when load is called', async () => {
    const mockData = { props: { documents: ['doc1', 'doc2'] } };
    (global.fetch as ReturnType<typeof vi.fn>).mockResolvedValueOnce({
      ok: true,
      headers: new Headers(),
      json: () => Promise.resolve(mockData),
    });

    const lazyUrls = { docs: '/api/docs' };
    const { result } = renderHook(() => useLazy<{ documents: string[] }>('docs', lazyUrls), {
      wrapper: createWrapper(),
    });

    const [, load] = result.current;
    load();

    await waitFor(() => {
      const [state] = result.current;
      expect(state.status).toBe('success');
    });

    const [state] = result.current;
    if (state.status === 'success') {
      expect(state.data).toEqual({ documents: ['doc1', 'doc2'] });
    }
  });
});
