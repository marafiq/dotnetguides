import React, { useEffect, useState } from 'react';
import { createRoot } from 'react-dom/client';
import {
  RouterProvider,
  createRouter,
  createRootRoute,
  createRoute,
  Link,
  Outlet
} from '@tanstack/react-router';

// RSC Client - interprets server component wire format
async function fetchRsc(feature: string, component: string, props?: object) {
  const url = `/api/rsc/${feature}/${component}`;
  const options: RequestInit = props
    ? { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(props) }
    : { method: 'GET' };

  const response = await fetch(url, options);
  if (!response.ok) throw new Error(`RSC fetch failed: ${response.statusText}`);

  const wireFormat = await response.text();
  return parseRscWireFormat(wireFormat);
}

// Parse RSC wire format: line-delimited JSON like 0:["$","Component",null,{props}]
function parseRscWireFormat(wireFormat: string): React.ReactNode {
  const lines = wireFormat.trim().split('\n');
  const nodes: Record<number, any> = {};

  for (const line of lines) {
    const colonIndex = line.indexOf(':');
    if (colonIndex === -1) continue;

    const index = parseInt(line.slice(0, colonIndex));
    const json = line.slice(colonIndex + 1);

    try {
      const [marker, type, key, props] = JSON.parse(json);
      if (marker === '$') {
        nodes[index] = { type, key, props };
      }
    } catch (e) {
      console.warn('Failed to parse RSC line:', line);
    }
  }

  // Convert to React elements
  return Object.values(nodes).map((node, i) =>
    React.createElement('div', { key: i, 'data-rsc-component': node.type },
      JSON.stringify(node.props, null, 2)
    )
  );
}

// Layout component
function RootLayout() {
  return (
    <div style={{ maxWidth: '1200px', margin: '0 auto', padding: '20px' }}>
      <header style={{ marginBottom: '30px', borderBottom: '1px solid #ddd', paddingBottom: '20px' }}>
        <h1 style={{ marginBottom: '10px' }}>Shalimar Sample App</h1>
        <nav style={{ display: 'flex', gap: '20px' }}>
          <Link to="/" style={{ color: '#0066cc', textDecoration: 'none' }}>Home</Link>
          <Link to="/users" style={{ color: '#0066cc', textDecoration: 'none' }}>Users</Link>
          <Link to="/dashboard" style={{ color: '#0066cc', textDecoration: 'none' }}>Dashboard</Link>
        </nav>
      </header>
      <main>
        <Outlet />
      </main>
    </div>
  );
}

// Home page
function HomePage() {
  return (
    <div>
      <h2>Welcome to Shalimar</h2>
      <p style={{ marginTop: '10px' }}>
        This sample app demonstrates the Shalimar Razor-React compiler.
      </p>
      <ul style={{ marginTop: '20px', paddingLeft: '20px' }}>
        <li>Razor files are the single source of truth</li>
        <li>TSX is generated alongside .razor files (vertical slices)</li>
        <li>@server components render to RSC wire format</li>
        <li>@client components are bundled by Vite</li>
      </ul>
    </div>
  );
}

// Users page - demonstrates @server component
function UsersPage() {
  const [rscContent, setRscContent] = useState<React.ReactNode>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    fetchRsc('Users', 'UserProfile', { Name: 'John Doe', Email: 'john@example.com', IsActive: true })
      .then(setRscContent)
      .catch(e => setError(e.message))
      .finally(() => setLoading(false));
  }, []);

  return (
    <div>
      <h2>Users (Server Component Demo)</h2>
      <p style={{ marginTop: '10px', color: '#666' }}>
        UserProfile is a @server component - rendered via RSC wire format.
      </p>
      <div style={{ marginTop: '20px', padding: '20px', background: '#fff', borderRadius: '8px', border: '1px solid #ddd' }}>
        {loading && <p>Loading server component...</p>}
        {error && <p style={{ color: 'red' }}>Error: {error}</p>}
        {rscContent && (
          <div>
            <h4 style={{ marginBottom: '10px' }}>RSC Wire Format Response:</h4>
            <pre style={{ background: '#f0f0f0', padding: '10px', borderRadius: '4px', overflow: 'auto' }}>
              {rscContent}
            </pre>
          </div>
        )}
      </div>
    </div>
  );
}

// Dashboard page - demonstrates @client component
function DashboardPage() {
  return (
    <div>
      <h2>Dashboard (Client Component Demo)</h2>
      <p style={{ marginTop: '10px', color: '#666' }}>
        Dashboard components are @client - bundled by Vite, hydrated on client.
      </p>
      <div style={{ marginTop: '20px', display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(200px, 1fr))', gap: '20px' }}>
        <div style={{ padding: '20px', background: '#fff', borderRadius: '8px', border: '1px solid #ddd' }}>
          <h3>Stats</h3>
          <p style={{ fontSize: '24px', fontWeight: 'bold', color: '#0066cc' }}>1,234</p>
          <p style={{ color: '#666' }}>Total Users</p>
        </div>
        <div style={{ padding: '20px', background: '#fff', borderRadius: '8px', border: '1px solid #ddd' }}>
          <h3>Revenue</h3>
          <p style={{ fontSize: '24px', fontWeight: 'bold', color: '#00aa00' }}>$12,345</p>
          <p style={{ color: '#666' }}>This Month</p>
        </div>
        <div style={{ padding: '20px', background: '#fff', borderRadius: '8px', border: '1px solid #ddd' }}>
          <h3>Orders</h3>
          <p style={{ fontSize: '24px', fontWeight: 'bold', color: '#ff6600' }}>567</p>
          <p style={{ color: '#666' }}>Pending</p>
        </div>
      </div>
    </div>
  );
}

// Define routes
const rootRoute = createRootRoute({ component: RootLayout });

const indexRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: '/',
  component: HomePage
});

const usersRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: '/users',
  component: UsersPage
});

const dashboardRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: '/dashboard',
  component: DashboardPage
});

const routeTree = rootRoute.addChildren([indexRoute, usersRoute, dashboardRoute]);
const router = createRouter({ routeTree });

// Render app
const container = document.getElementById('root');
if (container) {
  const root = createRoot(container);
  root.render(
    <React.StrictMode>
      <RouterProvider router={router} />
    </React.StrictMode>
  );
}
