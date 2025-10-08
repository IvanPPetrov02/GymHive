import { writable, derived, get } from 'svelte/store';
import { createAuth0Client, type Auth0Client, type RedirectLoginOptions, type GetTokenSilentlyOptions } from '@auth0/auth0-spa-js';
import { getAuthConfig, isAuthConfigured } from './config/env';

// =============================
// Stores
// =============================
export const auth0Client = writable<Auth0Client | null>(null);
export const isAuthenticated = writable(false);
export const user = writable<any | null>(null);
export const isLoading = writable(true);
export const accessToken = writable<string | null>(null);
export const authError = writable<string | null>(null);
export const authConfigMissing = writable(false);

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
// Internal
// =============================
const baseConfig = getAuthConfig();
const audience = 'https://api.gymhive.local'; // per requirements
let initStarted = false;

function unconfigured() {
  return !isAuthConfigured();
}

// =============================
// Initialization
// =============================
export async function initAuth() {
  if (initStarted) return; // idempotent
  initStarted = true;

  if (unconfigured()) {
    authConfigMissing.set(true);
    console.warn('[Auth] Missing Auth0 env vars (VITE_AUTH0_DOMAIN / VITE_AUTH0_CLIENT_ID).');
    isLoading.set(false);
    return;
  }

  try {
    const client = await createAuth0Client({
      domain: baseConfig.domain,
      clientId: baseConfig.clientId,
      authorizationParams: {
        audience, // fixed audience per requirement
        scope: baseConfig.scope || 'openid profile email',
        redirect_uri: window.location.origin + '/'
      },
      cacheLocation: 'localstorage',
      useRefreshTokens: true
    });
    auth0Client.set(client);

    // Handle redirect callback if returning from Auth0
    if (window.location.search.includes('code=') && window.location.search.includes('state=')) {
      try {
        const { appState } = await client.handleRedirectCallback();
        const targetHashRaw = (appState && (appState as any).target) || window.location.hash || '#/';
        const normalized = targetHashRaw.startsWith('#') ? targetHashRaw : '#' + targetHashRaw.replace(/^\//, '');
        window.history.replaceState({}, document.title, window.location.pathname + normalized);
      } catch (e: any) {
        console.error('[Auth] Redirect callback failed', e);
        authError.set(e?.message || 'Authentication callback failed');
      }
    } else if (window.location.search.includes('error=')) {
      const params = new URLSearchParams(window.location.search);
      const err = params.get('error_description') || params.get('error');
      if (err) authError.set(decodeURIComponent(err));
      window.history.replaceState({}, document.title, window.location.pathname + window.location.hash);
    }

    const authed = await client.isAuthenticated();
    isAuthenticated.set(authed);
    if (authed) {
      await hydrateUserAndToken(client);
    }
  } catch (e: any) {
    console.error('[Auth] Initialization error', e);
    authError.set(e?.message || 'Initialization failed');
  } finally {
    isLoading.set(false);
  }
}

async function hydrateUserAndToken(client: Auth0Client) {
  try {
    const u = await client.getUser();
    user.set(u);
  } catch (e) {
    console.warn('[Auth] Failed to load user profile', e);
  }
  try {
    const raw = await client.getTokenSilently({
      authorizationParams: { audience, scope: baseConfig.scope }
    } as GetTokenSilentlyOptions);
    const token = typeof raw === 'string' ? raw : (raw as any)?.access_token;
    if (token) accessToken.set(token);
  } catch (e) {
    console.warn('[Auth] Could not get initial access token', e);
  }
}

// =============================
// Auth Actions
// =============================
export async function login(targetHash?: string) {
  if (unconfigured()) {
    authConfigMissing.set(true);
    alert('Authentication not configured. Set VITE_AUTH0_DOMAIN and VITE_AUTH0_CLIENT_ID.');
    return;
  }
  if (!get(auth0Client) && !get(isLoading)) await initAuth();
  if (get(isLoading)) await waitForAuth();
  const client = get(auth0Client);
  if (!client) return console.error('[Auth] login() called but client missing');
  const currentHash = window.location.hash || '#/';
  const target = targetHash?.startsWith('#') ? targetHash : currentHash;
  const opts: RedirectLoginOptions = {
    authorizationParams: {
      redirect_uri: window.location.origin + '/',
      appState: { target }
    }
  } as any;
  await client.loginWithRedirect(opts);
}

export async function logout() {
  const client = get(auth0Client);
  if (!client) return console.warn('[Auth] logout() before init');
  client.logout({ logoutParams: { returnTo: window.location.origin + '/' } });
  isAuthenticated.set(false);
  user.set(null);
  accessToken.set(null);
}

export async function getAccessToken(options?: GetTokenSilentlyOptions): Promise<string | null> {
  const client = get(auth0Client);
  if (!client) return null;
  try {
    const raw = await client.getTokenSilently({
      authorizationParams: { audience, scope: baseConfig.scope, ...(options as any)?.authorizationParams },
      ...options
    } as GetTokenSilentlyOptions);
    const token = typeof raw === 'string' ? raw : (raw as any)?.access_token;
    if (token) {
      accessToken.set(token);
      return token;
    }
  } catch (e) {
    console.error('[Auth] getAccessToken failed', e);
  }
  return null;
}

export async function ensureAuthenticated(targetHash?: string): Promise<boolean> {
  await waitForAuth();
  if (!get(isAuthenticated)) {
    await login(targetHash);
    return false; // navigation will redirect
  }
  return true;
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
    if (!ok) return {}; // navigation will change after redirect
    return componentImport();
  };
}

// Optional periodic silent renewal (every 5 minutes) to keep token fresh
let renewInterval: number | null = null;
function startRenewLoop() {
  if (renewInterval) return;
  renewInterval = window.setInterval(async () => {
    if (get(isAuthenticated)) {
      await getAccessToken().catch(() => {});
    }
  }, 5 * 60 * 1000);
}

// Auto-start on first init
initAuth().then(startRenewLoop).catch(() => {});

// Expose audience for external api helpers
export function getApiAudience() { return audience; }

// Wrapper around client.getUser() for convenience
export async function getUser(forceRefresh = false) {
  if (!forceRefresh && get(user)) return get(user);
  const client = get(auth0Client);
  if (!client) return null;
  try {
    const u = await client.getUser();
    user.set(u);
    return u;
  } catch (e) {
    console.warn('[Auth] getUser failed', e);
    return null;
  }
}
