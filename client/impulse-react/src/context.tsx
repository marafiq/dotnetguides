import { createContext, useContext, useState, useCallback, type ReactNode } from 'react';
import type { ImpulsePayload, ImpulseConfig } from './types';

interface ImpulseContextValue<TContext = unknown> {
  /** The current app context from the server */
  context: TContext;
  /** The current bundle version */
  version: string;
  /** Update the context (typically after navigation) */
  setContext: (context: TContext) => void;
  /** Configuration */
  config: ImpulseConfig;
}

const ImpulseContext = createContext<ImpulseContextValue | null>(null);

interface ImpulseProviderProps<TContext> {
  initialPayload: ImpulsePayload<TContext>;
  config?: ImpulseConfig;
  children: ReactNode;
}

/**
 * Provider that holds the Impulse app context.
 */
export function ImpulseProvider<TContext>({
  initialPayload,
  config = {},
  children,
}: ImpulseProviderProps<TContext>) {
  const [context, setContext] = useState<TContext>(initialPayload.context);

  const value: ImpulseContextValue<TContext> = {
    context,
    version: initialPayload.version,
    setContext,
    config,
  };

  return (
    <ImpulseContext.Provider value={value as ImpulseContextValue}>
      {children}
    </ImpulseContext.Provider>
  );
}

/**
 * Hook to access the Impulse context.
 */
export function useImpulseContext<TContext = unknown>(): TContext {
  const ctx = useContext(ImpulseContext);
  if (!ctx) {
    throw new Error('useImpulseContext must be used within an ImpulseProvider');
  }
  return ctx.context as TContext;
}

/**
 * Hook to access the full Impulse state (for internal use).
 */
export function useImpulseState<TContext = unknown>(): ImpulseContextValue<TContext> {
  const ctx = useContext(ImpulseContext);
  if (!ctx) {
    throw new Error('useImpulseState must be used within an ImpulseProvider');
  }
  return ctx as ImpulseContextValue<TContext>;
}
