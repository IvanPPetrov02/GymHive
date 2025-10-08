// This file now re-exports the new consolidated Auth0 implementation from src/lib/auth.ts
// to preserve existing import paths (../auth/auth) used by components.
export * from '../auth';
export { isLoading as loading, ensureAuthenticated as requireAuth } from '../auth';
