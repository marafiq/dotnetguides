import { describe, it, expect } from 'vitest';
import { renderHook } from '@testing-library/react';
import { ImpulseProvider, useImpulseContext, useImpulseState } from './context';
import type { ImpulsePayload } from './types';
import type { ReactNode } from 'react';

describe('ImpulseProvider', () => {
  const createWrapper = (payload: ImpulsePayload) => {
    return ({ children }: { children: ReactNode }) => (
      <ImpulseProvider initialPayload={payload}>
        {children}
      </ImpulseProvider>
    );
  };

  it('provides context to children', () => {
    const payload: ImpulsePayload = {
      url: '/',
      version: '1.0.0',
      props: { message: 'Hello' },
      context: { user: { id: 1, name: 'Test' } },
    };

    const { result } = renderHook(() => useImpulseContext(), {
      wrapper: createWrapper(payload),
    });

    expect(result.current).toEqual({ user: { id: 1, name: 'Test' } });
  });

  it('provides version through useImpulseState', () => {
    const payload: ImpulsePayload = {
      url: '/',
      version: 'abc123',
      props: {},
      context: {},
    };

    const { result } = renderHook(() => useImpulseState(), {
      wrapper: createWrapper(payload),
    });

    expect(result.current.version).toBe('abc123');
  });

  it('throws error when useImpulseContext is called outside provider', () => {
    expect(() => {
      renderHook(() => useImpulseContext());
    }).toThrow('useImpulseContext must be used within an ImpulseProvider');
  });

  it('throws error when useImpulseState is called outside provider', () => {
    expect(() => {
      renderHook(() => useImpulseState());
    }).toThrow('useImpulseState must be used within an ImpulseProvider');
  });
});
