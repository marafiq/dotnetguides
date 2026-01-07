import { Outlet, Link } from '@tanstack/react-router';
import { useImpulseContext } from '@impulse/react';

interface AppContext {
  user: { id: number; name: string; role: string };
  permissions: string[];
  tenant: { id: number; name: string };
}

export function Layout() {
  const context = useImpulseContext<AppContext>();

  return (
    <div style={{ minHeight: '100vh', display: 'flex', flexDirection: 'column' }}>
      <header style={{
        backgroundColor: '#1a1a2e',
        color: 'white',
        padding: '1rem 2rem',
        display: 'flex',
        justifyContent: 'space-between',
        alignItems: 'center'
      }}>
        <nav style={{ display: 'flex', gap: '2rem', alignItems: 'center' }}>
          <Link to="/" style={{ color: 'white', textDecoration: 'none', fontWeight: 'bold', fontSize: '1.25rem' }}>
            Impulse Demo
          </Link>
          <Link to="/" style={{ color: '#a0a0a0', textDecoration: 'none' }}>
            Dashboard
          </Link>
          <Link to="/residents" style={{ color: '#a0a0a0', textDecoration: 'none' }}>
            Residents
          </Link>
        </nav>
        <div style={{ display: 'flex', gap: '1rem', alignItems: 'center' }}>
          <span style={{ color: '#a0a0a0' }}>{context.tenant.name}</span>
          <span style={{
            backgroundColor: '#4a4a6a',
            padding: '0.25rem 0.75rem',
            borderRadius: '9999px',
            fontSize: '0.875rem'
          }}>
            {context.user.name} ({context.user.role})
          </span>
        </div>
      </header>
      <main style={{ flex: 1, padding: '2rem', backgroundColor: '#f5f5f5' }}>
        <Outlet />
      </main>
      <footer style={{
        backgroundColor: '#1a1a2e',
        color: '#a0a0a0',
        padding: '1rem 2rem',
        textAlign: 'center',
        fontSize: '0.875rem'
      }}>
        Impulse Framework Demo - Built with .NET 10 + React 19 + TanStack Router
      </footer>
    </div>
  );
}
