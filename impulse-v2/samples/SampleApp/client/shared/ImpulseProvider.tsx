/**
 * Impulse React Provider
 * Provides the Impulse context to the component tree
 * This is the "Impulse way" - server-driven data fetching with proper React integration
 */

import React, { createContext, useContext, useMemo } from 'react';
import type { ImpulseContext } from '../impulse-runtime';
import { createImpulseContext } from '../impulse-runtime';

// ========================================
// Context Definition
// ========================================

const ImpulseCtx = createContext<ImpulseContext | null>(null);

// ========================================
// Provider Component
// ========================================

interface ImpulseProviderProps {
  children: React.ReactNode;
}

export function ImpulseProvider({ children }: ImpulseProviderProps) {
  // Memoize the context to avoid recreating on every render
  const ctx = useMemo(() => createImpulseContext(), []);

  return (
    <ImpulseCtx.Provider value={ctx}>
      {children}
    </ImpulseCtx.Provider>
  );
}

// ========================================
// Hook for accessing context
// ========================================

export function useImpulse(): ImpulseContext {
  const ctx = useContext(ImpulseCtx);
  if (!ctx) {
    throw new Error('useImpulse must be used within an ImpulseProvider');
  }
  return ctx;
}

// ========================================
// Re-export types for convenience
// ========================================

export type { ImpulseContext };
