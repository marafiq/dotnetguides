import { useState } from 'react';
import { ResidentsList } from './features/residents';
import { AdmissionWizard } from './features/admission';

type View = 'residents' | 'admission';

export function App() {
  const [currentView, setCurrentView] = useState<View>('residents');

  return (
    <div className="app" data-testid="app">
      <header className="app-header">
        <h1>Green Valley Care Home</h1>
        <nav className="app-nav" data-testid="app-nav">
          <button
            className={currentView === 'residents' ? 'active' : ''}
            onClick={() => setCurrentView('residents')}
            data-testid="nav-residents"
          >
            Residents
          </button>
          <button
            className={currentView === 'admission' ? 'active' : ''}
            onClick={() => setCurrentView('admission')}
            data-testid="nav-admission"
          >
            New Admission
          </button>
        </nav>
      </header>

      <main className="app-main" data-testid="app-main">
        {currentView === 'residents' && <ResidentsList />}
        {currentView === 'admission' && (
          <AdmissionWizard onComplete={() => setCurrentView('residents')} />
        )}
      </main>
    </div>
  );
}
