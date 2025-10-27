import { writable, derived, get } from 'svelte/store';
import { getApiBase } from './api';

// =============================
// Types
// =============================
export interface User {
  uuid: string;
  email: string;
  name: string;
  surname: string;
  isActive: boolean;
  createdAt: string;
  role: string;
}

export interface LoginCredentials {
  email: string;
  password: string;
}

export interface RegisterData {
  email: string;
  password: string;
  name: string;
  surname: string;
}

// =============================
// Stores
// =============================
export const isAuthenticated = writable(false);
export const user = writable<User | null>(null);
export const isLoading = writable(true);
export const accessToken = writable<string | null>(null);
export const authError = writable<string | null>(null);

export const authState = derived(
  [isAuthenticated, user, isLoading, authError],
  ([$isAuthed, $user, $loading, $err]) => ({
    isAuthenticated: $isAuthed,
    user: $user,
    loading: $loading,
    error: $err
  })
);

// =============================
// Constants
// =============================
const TOKEN_KEY = 'gymhive_access_token';
const USER_KEY = 'gymhive_user';

// =============================
// Internal Helpers
// =============================
function saveToStorage(token: string, userData: User) {
  try {
    localStorage.setItem(TOKEN_KEY, token);
    localStorage.setItem(USER_KEY, JSON.stringify(userData));
  } catch (e) {
    console.warn('[Auth] Failed to save to localStorage', e);
  }
}

function clearStorage() {
  try {
    localStorage.removeItem(TOKEN_KEY);
    localStorage.removeItem(USER_KEY);
  } catch (e) {
    console.warn('[Auth] Failed to clear localStorage', e);
  }
}

function loadFromStorage(): { token: string | null; user: User | null } {
  try {
    const token = localStorage.getItem(TOKEN_KEY);
    const userJson = localStorage.getItem(USER_KEY);
    const userData = userJson ? JSON.parse(userJson) : null;
    return { token, user: userData };
  } catch (e) {
    console.warn('[Auth] Failed to load from localStorage', e);
    return { token: null, user: null };
  }
}

// =============================
// Initialization
// =============================
export async function initAuth() {
  isLoading.set(true);
  authError.set(null);

  try {
    const { token, user: storedUser } = loadFromStorage();

    if (token && storedUser) {
      // Validate token by fetching user profile from API
      const apiBase = getApiBase();
      if (!apiBase) {
        console.warn('[Auth] No API base URL configured');
        clearStorage();
        isAuthenticated.set(false);
        isLoading.set(false);
        return;
      }

      try {
        const response = await fetch(`${apiBase}/api/auth/GetUser`, {
          headers: {
            'Authorization': `Bearer ${token}`,
            'Content-Type': 'application/json'
          }
        });

        if (response.ok) {
          const userData = await response.json();
          user.set(userData);
          accessToken.set(token);
          isAuthenticated.set(true);
        } else {
          // Token invalid, clear storage
          clearStorage();
          isAuthenticated.set(false);
        }
      } catch (e) {
        console.warn('[Auth] Failed to validate token', e);
        clearStorage();
        isAuthenticated.set(false);
      }
    } else {
      isAuthenticated.set(false);
    }
  } catch (e: any) {
    console.error('[Auth] Initialization error', e);
    authError.set(e?.message || 'Initialization failed');
    isAuthenticated.set(false);
  } finally {
    isLoading.set(false);
  }
}

// =============================
// Auth Actions
// =============================
export async function login(credentials: LoginCredentials): Promise<boolean> {
  authError.set(null);
  const apiBase = getApiBase();

  if (!apiBase) {
    authError.set('API base URL not configured');
    return false;
  }

  try {
    const response = await fetch(`${apiBase}/api/auth/login`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(credentials)
    });

    if (!response.ok) {
      const error = await response.json().catch(() => ({ message: 'Login failed' }));
      authError.set(error.message || `Login failed: ${response.status}`);
      return false;
    }

    const data = await response.json();
    const token = data.token;

    if (!token) {
      authError.set('No token received from server');
      return false;
    }

    // Fetch user data after successful login
    try {
      const userResponse = await fetch(`${apiBase}/api/auth/GetUser`, {
        headers: {
          'Authorization': `Bearer ${token}`,
          'Content-Type': 'application/json'
        }
      });

      if (userResponse.ok) {
        const userData = await userResponse.json();
        saveToStorage(token, userData);
        accessToken.set(token);
        user.set(userData);
        isAuthenticated.set(true);
        return true;
      } else {
        authError.set('Failed to fetch user data');
        return false;
      }
    } catch (e: any) {
      console.error('[Auth] Failed to fetch user data', e);
      authError.set('Failed to fetch user data');
      return false;
    }
  } catch (e: any) {
    console.error('[Auth] Login error', e);
    authError.set(e?.message || 'Network error during login');
    return false;
  }
}

export async function register(data: RegisterData): Promise<boolean> {
  authError.set(null);
  const apiBase = getApiBase();

  if (!apiBase) {
    authError.set('API base URL not configured');
    return false;
  }

  try {
    const response = await fetch(`${apiBase}/api/auth/register`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(data)
    });

    if (!response.ok) {
      const error = await response.json().catch(() => ({ message: 'Registration failed' }));
      authError.set(error.message || `Registration failed: ${response.status}`);
      return false;
    }

    const responseData = await response.json();
    
    // After registration, automatically log in
    if (responseData.message === 'User created') {
      return await login({ email: data.email, password: data.password });
    }

    authError.set('Registration failed: ' + (responseData.message || 'Unknown error'));
    return false;
  } catch (e: any) {
    console.error('[Auth] Registration error', e);
    authError.set(e?.message || 'Network error during registration');
    return false;
  }
}

export async function logout() {
  const apiBase = getApiBase();
  
  // Call logout endpoint if API is available
  if (apiBase) {
    try {
      await fetch(`${apiBase}/api/auth/logout`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' }
      });
    } catch (e) {
      console.warn('[Auth] Logout API call failed', e);
    }
  }

  clearStorage();
  isAuthenticated.set(false);
  user.set(null);
  accessToken.set(null);
  authError.set(null);
}

export async function getAccessToken(): Promise<string | null> {
  const token = get(accessToken);
  if (token) return token;

  // Try to load from storage
  const { token: storedToken } = loadFromStorage();
  if (storedToken) {
    accessToken.set(storedToken);
    return storedToken;
  }

  return null;
}

export async function ensureAuthenticated(): Promise<boolean> {
  await waitForAuth();
  return get(isAuthenticated);
}

export async function waitForAuth(timeoutMs = 8000) {
  const start = Date.now();
  while (get(isLoading) && Date.now() - start < timeoutMs) {
    await new Promise(r => setTimeout(r, 50));
  }
  return {
    isAuthenticated: get(isAuthenticated),
    user: get(user),
    error: get(authError)
  };
}

// =============================
// Auth Guard (Svelte SPA Router helper)
// =============================
export function authGuard(componentImport: () => Promise<any>) {
  return async () => {
    const ok = await ensureAuthenticated();
    if (!ok) {
      // Redirect to login
      window.location.hash = '#/login';
      return {};
    }
    return componentImport();
  };
}

// Refresh user profile from API
export async function getUser(forceRefresh = false) {
  if (!forceRefresh && get(user)) return get(user);

  const token = await getAccessToken();
  if (!token) return null;

  const apiBase = getApiBase();
  if (!apiBase) return null;

  try {
    const response = await fetch(`${apiBase}/api/users/me`, {
      headers: {
        'Authorization': `Bearer ${token}`,
        'Content-Type': 'application/json'
      }
    });

    if (response.ok) {
      const userData = await response.json();
      user.set(userData);
      return userData;
    }
  } catch (e) {
    console.warn('[Auth] getUser failed', e);
  }

  return null;
}

// Legacy alias for backward compatibility
export async function requireAuth(redirectPath?: string): Promise<boolean> {
  const authed = await ensureAuthenticated();
  if (!authed && redirectPath) {
    window.location.hash = '#/login';
  }
  return authed;
}

// Auto-initialize on module load
initAuth().catch(console.error);
