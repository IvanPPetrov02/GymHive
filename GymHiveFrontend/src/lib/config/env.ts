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
  // Always use API Gateway - direct service URLs should not be used from frontend
    // Runtime detection: prefer window.GYMHIVE_CONFIG (set by Kubernetes), fallback to build-time env, then localhost
  apiGateway: normalizeUrl(
    (typeof window !== 'undefined' && (window as any).GYMHIVE_CONFIG?.API_GATEWAY_URL !== '__API_GATEWAY_URL__') 
      ? (window as any).GYMHIVE_CONFIG?.API_GATEWAY_URL 
      : import.meta.env.VITE_API_GATEWAY_URL || 'http://localhost:5000'
  ),
  gymsService: '', // Not used - all requests go through gateway
  identityService: '', // Not used - all requests go through gateway
  mediaService: '',
  notificationsService: '',
  socialFeedService: '',
  workoutService: ''
};

const buildConfig: BuildConfig = {
  mode: import.meta.env.MODE,
  dev: import.meta.env.DEV,
  prod: import.meta.env.PROD,
  version: (import.meta as any).env.VITE_APP_VERSION || '0.0.0',
  buildTime: new Date().toISOString()
};

const appConfig: AppConfig = {
  services: servicesConfig,
  build: buildConfig
};

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
  validateServices(appConfig.services, issues);
  validateBuild(appConfig.build, issues);
  collectedIssues = issues;
  return issues;
}

(function runValidationOnce() {
  // Retain original behavior (console output) plus structured issue capture
  performFullValidation();
})();

export function getAppConfig() { return appConfig; }
export function getServicesConfig() { return servicesConfig; }
export function getBuildConfig() { return buildConfig; }
export function getValidationIssues() { return performFullValidation(); }
