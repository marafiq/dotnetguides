import { createContext, useContext, useMemo, type ReactNode } from 'react';
import { createImpulseContext, type ImpulseContextValue } from '../impulse-runtime';

export type { ImpulseContextValue as ImpulseContext };

const ImpulseContext = createContext<ImpulseContextValue | null>(null);

interface ImpulseProviderProps {
  children: ReactNode;
}

export function ImpulseProvider({ children }: ImpulseProviderProps) {
  const ctx = useMemo(() => createImpulseContext(), []);

  return (
    <ImpulseContext.Provider value={ctx}>
      {children}
    </ImpulseContext.Provider>
  );
}

export function useImpulse(): ImpulseContextValue {
  const ctx = useContext(ImpulseContext);
  if (!ctx) {
    throw new Error('useImpulse must be used within ImpulseProvider');
  }
  return ctx;
}
