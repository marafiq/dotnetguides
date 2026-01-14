import { Store } from '@tanstack/store';
import { useStore } from '@tanstack/react-store';

//  User State Types

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

//  Store Instance

export const userStore = new Store({
  state: {
    name: '',
    email: '',
    isAuthenticated: false,
    roles: [],
    preferences: {
      theme: 'light',
      language: 'en',
      notifications: true
    }
  }
});

//  Actions

export const setUser = (user: Partial<UserState>) => {
  userStore.setState((state) => ({
    ...state,
    ...user,
    isAuthenticated: true
  }));
};

export const logout = () => {
  userStore.setState(() => ({
    name: '',
    email: '',
    isAuthenticated: false,
    roles: [],
    preferences: {
      theme: 'light',
      language: 'en',
      notifications: true
    }
  }));
};

export const updatePreferences = (newPrefs: Partial<UserPreferences>) => {
  userStore.setState((state) => ({
    ...state,
    preferences: {
      ...state.preferences,
      ...newPrefs
    }
  }));
};

//  Selectors

export const selectIsAdmin = (state: UserState): boolean => {
  return state.roles.includes('admin');
};

//  React Hook

export const useUserStore = <T>(selector?: (state: UserState) => T) => {
  return useStore(userStore, selector);
};
