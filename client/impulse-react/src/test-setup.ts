import '@testing-library/react';

// Mock window.fetch
global.fetch = vi.fn();

// Mock window.location
Object.defineProperty(window, 'location', {
  value: {
    reload: vi.fn(),
    href: 'http://localhost:3000',
    pathname: '/',
    search: '',
  },
  writable: true,
});
