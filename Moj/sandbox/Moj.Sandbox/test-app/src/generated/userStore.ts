import { Store } from '@tanstack/store';

import { useStore } from '@tanstack/react-store';

export interface UserPreferences {
  theme: string;
  language: string;
  notifications: boolean;
}

export interface UserState {
  name: string;
  email: string;
  isAuthenticated: boolean;
  roles: string[];
  preferences: UserPreferences;
}

export const userStore = new Store({ state: {
name: '',
email: '',
isAuthenticated: false,
roles: [],
preferences: { theme: 'light', language: 'en', notifications: true }
} });

export const setUser = (user: unknown) => userStore.setState((state: unknown) => ({ ...state, ...user, isAuthenticated: true }));

export const logout = () => ({
name: '',
email: '',
isAuthenticated: false,
roles: [],
preferences: { theme: 'light', language: 'en', notifications: true }
});

export const updatePreferences = (newPrefs: unknown) => userStore.setState((state: unknown) => ({ ...state, preferences: { ...state.preferences, ...newPrefs } }));

export const selectIsAdmin = (state: unknown) => state.roles.includes('admin');

export const useUserStore = () => useStore(userStore);