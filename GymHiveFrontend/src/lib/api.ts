import { getAccessToken } from './auth';
import { getServicesConfig } from './config/env';

// Determine API base: prefer apiGateway, fallback to VITE_API_URL env already consumed in env.ts
const services = getServicesConfig();
const API_BASE = services.apiGateway || '';

if (!API_BASE) {
  console.warn('[API] No API base URL configured. Set VITE_API_GATEWAY_URL or VITE_API_URL.');
}

export function getApiBase() { return API_BASE; }

export interface ApiError extends Error {
  status: number;
  body?: any;
}

async function buildHeaders(extra?: HeadersInit): Promise<Headers> {
  const headers = new Headers(extra || {});
  if (!headers.has('Content-Type')) headers.set('Content-Type', 'application/json');
  const token = await getAccessToken();
  if (token) headers.set('Authorization', `Bearer ${token}`);
  return headers;
}

function fullUrl(path: string) {
  if (/^https?:/i.test(path)) return path; // absolute
  const base = API_BASE.replace(/\/$/, '');
  const rel = path.startsWith('/') ? path : '/' + path;
  return base + rel;
}

export async function apiFetch(path: string, init: RequestInit = {}) {
  const headers = await buildHeaders(init.headers);
  const response = await fetch(fullUrl(path), { ...init, headers });
  if (!response.ok) {
    const err: ApiError = Object.assign(new Error(`API ${response.status} ${response.statusText}`), {
      status: response.status,
      body: null as any
    });
    try { err.body = await response.clone().json(); } catch { /* ignore */ }
    throw err;
  }
  return response;
}

export async function getJson<T = any>(path: string): Promise<T> {
  const res = await apiFetch(path, { method: 'GET' });
  return res.json();
}

export async function postJson<T = any, B = any>(path: string, body: B, init: RequestInit = {}): Promise<T> {
  const res = await apiFetch(path, { ...init, method: 'POST', body: JSON.stringify(body) });
  return res.json();
}

export async function putJson<T = any, B = any>(path: string, body: B, init: RequestInit = {}): Promise<T> {
  const res = await apiFetch(path, { ...init, method: 'PUT', body: JSON.stringify(body) });
  return res.json();
}

export async function del(path: string): Promise<void> {
  await apiFetch(path, { method: 'DELETE' });
}

export async function deleteRequest(path: string): Promise<void> {
  await apiFetch(path, { method: 'DELETE' });
}

