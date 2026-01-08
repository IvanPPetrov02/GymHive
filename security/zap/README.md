# OWASP ZAP (Baseline)

This folder contains a minimal, repeatable ZAP scan runner for GymHive.

## What it does

- Runs an **unauthenticated OWASP ZAP baseline scan** against the GymHive ingress URL.
- Produces HTML + JSON + Markdown reports under `security/zap/results/`.

## How the analysis was done (methodology)

Two scans are supported:

1) **Baseline scan (unauthenticated)**
- Tool: OWASP ZAP (Docker image `zaproxy/zap-stable`)
- Script: `zap-baseline.py`
- Target: the GymHive ingress base URL (e.g. `https://<host>/`)
- Purpose: quick passive checks + a light spider of publicly reachable pages/endpoints.

2) **API scan (OpenAPI-driven, unauthenticated where possible)**
- Tool: OWASP ZAP (Docker image `zaproxy/zap-stable`)
- Script: `zap-api-scan.py`
- Target: OpenAPI/Swagger spec when available (best coverage), otherwise falls back to scanning `https://<host>/api/`.
- Purpose: enumerate API endpoints from the spec and run ZAP’s API scan checks.

Both scripts:
- Auto-discover the ingress host via `kubectl -n gymhive get ingress gymhive-ingress` (unless you pass `-TargetUrl`).
- Write reports to `security/zap/results/`.

## Run

From repo root:

```powershell
./security/zap/run-zap-baseline.ps1
```

## API scan

Runs an API-focused scan. It will try to auto-detect an OpenAPI spec at common URLs like `/swagger/v1/swagger.json` and `/openapi.json` on your HTTPS ingress. If no spec is found, it falls back to scanning `/api/` directly.

```powershell
./security/zap/run-zap-api.ps1
```

Optional:

```powershell
# Explicitly provide OpenAPI URL
./security/zap/run-zap-api.ps1 -OpenApiUrl "https://gymhive.34.8.235.214.nip.io/swagger/v1/swagger.json"

# Explicitly provide API base URL
./security/zap/run-zap-api.ps1 -TargetUrl "https://gymhive.34.8.235.214.nip.io/api/"
```

Optional:

```powershell
# Force HTTP instead of HTTPS
./security/zap/run-zap-baseline.ps1 -UseHttp

# Scan a specific URL
./security/zap/run-zap-baseline.ps1 -TargetUrl "https://gymhive.34.8.235.214.nip.io/"
```

## Output

Reports are written to:

- `security/zap/results/zap-baseline-<timestamp>.html`
- `security/zap/results/zap-baseline-<timestamp>.json`
- `security/zap/results/zap-baseline-<timestamp>.md`

API scan reports are written to:

- `security/zap/results/zap-api-<timestamp>.html`
- `security/zap/results/zap-api-<timestamp>.json`
- `security/zap/results/zap-api-<timestamp>.md`

## Results (Jan 8, 2026)

These were the results from running the scripts against your current ingress host:

- Ingress host: `gymhive.34.8.235.214.nip.io`
- Baseline scan target: `http://gymhive.34.8.235.214.nip.io/` (HTTP run)
- API scan target: `https://gymhive.34.8.235.214.nip.io/api/`
- OpenAPI detected at: `https://gymhive.34.8.235.214.nip.io/swagger/v1/swagger.json`

### Baseline scan summary

- **FAIL:** 0
- **WARN:** 9
- Key warnings (typical hardening items):
	- Missing Anti-clickjacking header (X-Frame-Options / CSP frame-ancestors)
	- `X-Content-Type-Options` missing
	- `Content-Security-Policy` header not set
	- `Permissions-Policy` header not set
	- `Server` header leaks version information
	- Non-storable content / caching directives
	- Some browser isolation-related informational warnings

### API scan (OpenAPI-driven) summary

- **FAIL:** 0
- **WARN:** 8
- Notes:
	- `/api` returns `401` (protected), so unauthenticated scanning is limited to what’s publicly reachable.
	- OpenAPI/Swagger spec was reachable and used to enumerate endpoints.
	- Main warnings were again header hardening on the Swagger/OpenAPI response (CSP/HSTS/X-Frame/X-Content-Type-Options/Permissions-Policy, Server header), plus “Unexpected Content-Type” on some responses.

### Where to see full details

Open the generated HTML reports in `security/zap/results/` for the full list of alerts and affected URLs.
