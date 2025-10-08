export interface AuthConfig {
  domain: string;
  clientId: string;
  audience?: string;
  scope: string;
}

// Build-time injected via Vite (must start with VITE_ to be exposed to client)
const authConfig: AuthConfig = {
  domain: import.meta.env.VITE_AUTH0_DOMAIN || '',
  clientId: import.meta.env.VITE_AUTH0_CLIENT_ID || '',
  audience: import.meta.env.VITE_AUTH0_AUDIENCE || undefined,
  scope: import.meta.env.VITE_AUTH0_SCOPE || 'openid profile email'
};

export interface ServicesConfig {
  apiGateway: string;        // Main gateway base URL
  gymsService: string;       // Optional direct service URL
  identityService: string;   // Optional direct service URL
  mediaService: string;
  notificationsService: string;
  socialFeedService: string;
  workoutService: string;
}

export interface BuildConfig {
  mode: string;
  dev: boolean;
  prod: boolean;
  version: string; // from env or static fallback
  buildTime: string; // ISO timestamp at bundle
}

export interface AppConfig {
  auth: AuthConfig;
  services: ServicesConfig;
  build: BuildConfig;
}

// Added: structured validation issue representation
export interface ValidationIssue {
  section: string;
  message: string;
  severity: 'warning' | 'error';
}

function normalizeUrl(v: string): string {
  if (!v) return '';
  try { const url = new URL(v); return url.toString().replace(/\/$/, ''); } catch { return v.replace(/\/$/, ''); }
}

const servicesConfig: ServicesConfig = {
  apiGateway: normalizeUrl(import.meta.env.VITE_API_GATEWAY_URL || import.meta.env.VITE_API_URL || ''),
  gymsService: normalizeUrl(import.meta.env.VITE_GYMS_SERVICE_URL || ''),
  identityService: normalizeUrl(import.meta.env.VITE_IDENTITY_SERVICE_URL || ''),
  mediaService: normalizeUrl(import.meta.env.VITE_MEDIA_SERVICE_URL || ''),
  notificationsService: normalizeUrl(import.meta.env.VITE_NOTIFICATIONS_SERVICE_URL || ''),
  socialFeedService: normalizeUrl(import.meta.env.VITE_SOCIAL_FEED_SERVICE_URL || ''),
  workoutService: normalizeUrl(import.meta.env.VITE_WORKOUT_SERVICE_URL || '')
};

const buildConfig: BuildConfig = {
  mode: import.meta.env.MODE,
  dev: import.meta.env.DEV,
  prod: import.meta.env.PROD,
  version: (import.meta as any).env.VITE_APP_VERSION || '0.0.0',
  buildTime: new Date().toISOString()
};

const appConfig: AppConfig = {
  auth: authConfig,
  services: servicesConfig,
  build: buildConfig
};

function validateAuth(c: AuthConfig, issues?: ValidationIssue[]) {
  const problems: string[] = [];
  if (!c.domain) problems.push('Auth0 domain missing (VITE_AUTH0_DOMAIN)');
  if (!c.clientId) problems.push('Auth0 client id missing (VITE_AUTH0_CLIENT_ID)');
  if (c.domain.includes('your-tenant')) problems.push('Auth0 domain still placeholder');
  if (c.clientId === 'YOUR_CLIENT_ID') problems.push('Auth0 client id placeholder');
  if (problems.length) {
    const msg = '[AppConfig][Auth] Issues:\n - ' + problems.join('\n - ');
    if (issues) problems.forEach(p => issues.push({ section: 'auth', message: p, severity: 'warning' }));
    if (import.meta.env.DEV) console.warn(msg); else console.error(msg);
  }
}

function validateServices(s: ServicesConfig, issues?: ValidationIssue[]) {
  if (!s.apiGateway && import.meta.env.DEV) {
    const msg = 'VITE_API_GATEWAY_URL not set. Direct service URLs will be used if provided.';
    issues?.push({ section: 'services', message: msg, severity: 'warning' });
    console.warn('[AppConfig][Services] ' + msg);
  }
  // Validate each URL if provided
  (Object.keys(s) as (keyof ServicesConfig)[]).forEach(key => {
    const value = s[key];
    if (!value) return;
    try { new URL(value); } catch {
      const msg = `Service URL for ${String(key)} is not a valid absolute URL: ${value}`;
      issues?.push({ section: 'services', message: msg, severity: 'error' });
      console.error('[AppConfig][Services] ' + msg);
    }
  });
  // Ensure at least one endpoint strategy exists
  const anyDirect = Object.entries(s).some(([k,v]) => k !== 'apiGateway' && !!v);
  if (!s.apiGateway && !anyDirect) {
    const msg = 'No API gateway (VITE_API_GATEWAY_URL) nor any direct service URLs configured.';
    issues?.push({ section: 'services', message: msg, severity: 'error' });
    console.error('[AppConfig][Services] ' + msg);
  }
}

function validateBuild(b: BuildConfig, issues?: ValidationIssue[]) {
  // rudimentary semver check
  const semverRe = /^\d+\.\d+\.\d+(?:[-+][0-9A-Za-z-.]+)?$/;
  if (!semverRe.test(b.version)) {
    const msg = `Build version '${b.version}' is not a simple semver (override with VITE_APP_VERSION).`;
    issues?.push({ section: 'build', message: msg, severity: 'warning' });
    if (import.meta.env.DEV) console.warn('[AppConfig][Build] ' + msg);
  }
  // buildTime parsable
  if (Number.isNaN(Date.parse(b.buildTime))) {
    const msg = 'Build time is not a valid ISO timestamp: ' + b.buildTime;
    issues?.push({ section: 'build', message: msg, severity: 'error' });
    console.error('[AppConfig][Build] ' + msg);
  }
  if (b.dev === b.prod) {
    const msg = 'Both dev and prod flags are identical; expected exactly one to be true.';
    issues?.push({ section: 'build', message: msg, severity: 'warning' });
    if (import.meta.env.DEV) console.warn('[AppConfig][Build] ' + msg);
  }
}

// Collect all validation issues at module init
let collectedIssues: ValidationIssue[] | null = null;
function performFullValidation(): ValidationIssue[] {
  if (collectedIssues) return collectedIssues; // idempotent
  const issues: ValidationIssue[] = [];
  validateAuth(appConfig.auth, issues);
  validateServices(appConfig.services, issues);
  validateBuild(appConfig.build, issues);
  collectedIssues = issues;
  return issues;
}

(function runValidationOnce() {
  // Retain original behavior (console output) plus structured issue capture
  performFullValidation();
})();

export function getAppConfig(): AppConfig { return appConfig; }
export function getServicesConfig(): ServicesConfig { return appConfig.services; }
export function getBuildConfig(): BuildConfig { return appConfig.build; }

// Backwards compatibility (used by auth.ts)
export function getAuthConfig(): AuthConfig { return appConfig.auth; }
export function isAuthConfigured(): boolean { return isAuthConfiguredOriginal(); }

// New: expose validation utilities
export function getValidationIssues(): ValidationIssue[] { return performFullValidation().slice(); }
export function isConfigValid(): boolean { return getValidationIssues().every(i => i.severity !== 'error'); }

// New: convenience accessor for service URLs by key
export type ServiceName = keyof ServicesConfig;
export function getServiceUrl(name: ServiceName): string { return appConfig.services[name]; }

// Preserve original implementation name to avoid recursion
function isAuthConfiguredOriginal(): boolean {
  if (!authConfig.domain || !authConfig.clientId) return false;
  if (authConfig.domain.includes('your-tenant') || authConfig.clientId === 'YOUR_CLIENT_ID') return false;
  return true;
}
