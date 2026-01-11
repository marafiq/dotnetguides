import { useState } from 'react';
import { ResidentsList } from './components/ResidentsList';
import { AdmissionWizard } from './components/AdmissionWizard';

type View = 'residents' | 'admission';

export default function App() {
  const [currentView, setCurrentView] = useState<View>('residents');

  return (
    <div className="app">
      <header className="app-header">
        <h1>Green Valley Care Home</h1>
        <nav className="app-nav">
          <button
            className={currentView === 'residents' ? 'active' : ''}
            onClick={() => setCurrentView('residents')}
          >
            Residents
          </button>
          <button
            className={currentView === 'admission' ? 'active' : ''}
            onClick={() => setCurrentView('admission')}
          >
            New Admission
          </button>
        </nav>
      </header>

      <main className="app-main">
        {currentView === 'residents' && <ResidentsList />}
        {currentView === 'admission' && (
          <AdmissionWizard onComplete={() => setCurrentView('residents')} />
        )}
      </main>
    </div>
  );
}
