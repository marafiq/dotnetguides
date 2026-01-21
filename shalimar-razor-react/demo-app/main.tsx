import React from 'react';
import { createRoot } from 'react-dom/client';

// Import generated components from Razor files
// These are compiled by Shalimar: .razor -> .tsx
import { UserProfile } from './Features/Users/UserProfile';
import { ProductCard } from './Features/Products/ProductCard';

// Sample data
const sampleUser = {
  Name: 'Sarah Johnson',
  Email: 'sarah.johnson@example.com',
  AvatarUrl: 'https://i.pravatar.cc/150?img=47',
  IsVerified: true
};

const sampleProducts = [
  {
    Name: 'Wireless Headphones',
    Description: 'Premium noise-canceling wireless headphones with 30-hour battery life.',
    ImageUrl: 'https://picsum.photos/seed/headphones/400/300',
    Price: 299.99,
    InStock: true
  },
  {
    Name: 'Smart Watch',
    Description: 'Advanced fitness tracking with heart rate monitor and GPS.',
    ImageUrl: 'https://picsum.photos/seed/watch/400/300',
    Price: 449.99,
    InStock: true
  },
  {
    Name: 'Laptop Stand',
    Description: 'Ergonomic aluminum laptop stand with adjustable height.',
    ImageUrl: 'https://picsum.photos/seed/stand/400/300',
    Price: 79.99,
    InStock: false
  }
];

function App() {
  return (
    <div style={{ maxWidth: '1200px', margin: '0 auto', padding: '40px 20px' }}>
      {/* Header */}
      <header style={{ marginBottom: '40px', textAlign: 'center' }}>
        <h1 style={{ fontSize: '36px', marginBottom: '8px', color: '#333' }}>
          Shalimar Demo
        </h1>
        <p style={{ color: '#666', fontSize: '18px' }}>
          Razor files compiled to React TSX components
        </p>
        <div style={{ marginTop: '16px', padding: '12px 20px', background: '#e8f5e9', borderRadius: '8px', display: 'inline-block' }}>
          <code style={{ color: '#2e7d32' }}>
            .razor → Shalimar Compiler → .tsx → Vite → Browser
          </code>
        </div>
      </header>

      {/* User Profile Section - @server component */}
      <section style={{ marginBottom: '48px' }}>
        <h2 style={{ fontSize: '24px', marginBottom: '16px', color: '#333' }}>
          User Profile <span style={{ fontSize: '14px', color: '#666', fontWeight: 'normal' }}>(@server component)</span>
        </h2>
        <UserProfile
          Name={sampleUser.Name}
          Email={sampleUser.Email}
          AvatarUrl={sampleUser.AvatarUrl}
          IsVerified={sampleUser.IsVerified}
        />
      </section>

      {/* Products Section - @client components */}
      <section>
        <h2 style={{ fontSize: '24px', marginBottom: '16px', color: '#333' }}>
          Products <span style={{ fontSize: '14px', color: '#666', fontWeight: 'normal' }}>(@client components)</span>
        </h2>
        <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fill, minmax(280px, 1fr))', gap: '24px' }}>
          {sampleProducts.map((product, index) => (
            <ProductCard
              key={index}
              Name={product.Name}
              Description={product.Description}
              ImageUrl={product.ImageUrl}
              Price={product.Price}
              InStock={product.InStock}
            />
          ))}
        </div>
      </section>

      {/* Footer */}
      <footer style={{ marginTop: '60px', textAlign: 'center', padding: '20px', borderTop: '1px solid #e0e0e0' }}>
        <p style={{ color: '#666' }}>
          Components generated from <strong>.razor</strong> files by Shalimar Compiler
        </p>
      </footer>
    </div>
  );
}

// Render
const container = document.getElementById('root');
if (container) {
  const root = createRoot(container);
  root.render(
    <React.StrictMode>
      <App />
    </React.StrictMode>
  );
}
