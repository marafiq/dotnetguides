import { mount } from '@impulse/react';
import { App } from './App';

// Mount the Impulse application
const payload = mount(App, {
  rootElementId: 'app',
  config: {
    onVersionMismatch: () => {
      console.log('Version mismatch detected, reloading...');
    },
    onNavigationError: (error) => {
      console.error('Navigation error:', error);
    },
  },
});

if (payload) {
  console.log('Impulse app mounted with payload:', payload);
}
