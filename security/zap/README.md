# OWASP ZAP scans (GymHive)

This folder contains repeatable OWASP ZAP scan runners for GymHive.

## What was done

- Added a baseline scan runner (unauthenticated) for the public ingress.
- Added an API scan runner that uses the OpenAPI/Swagger spec when available (best coverage).
- Reports are generated in `security/zap/results/` (HTML + JSON + Markdown).

## Scripts

- `run-zap-baseline.ps1` — runs `zap-baseline.py` against the ingress root
- `run-zap-api.ps1` — runs `zap-api-scan.py` using OpenAPI, else falls back to scanning `/api/`

Both scripts use the Docker image `zaproxy/zap-stable`.

## How it works

1. Determine target URL
   - If `-TargetUrl` is provided, use it.
   - Otherwise, auto-discover the ingress host via `kubectl`.
2. Run ZAP in a disposable Docker container.
3. Write reports to `security/zap/results/`.

## Run baseline scan

From repo root:

```powershell
./security/zap/run-zap-baseline.ps1
```

Common options:

```powershell
./security/zap/run-zap-baseline.ps1 -TargetUrl "https://<host>/"
./security/zap/run-zap-baseline.ps1 -UseHttp
```

## Run API scan

From repo root:

```powershell
./security/zap/run-zap-api.ps1
```

Common options:

```powershell
./security/zap/run-zap-api.ps1 -OpenApiUrl "https://<host>/swagger/v1/swagger.json"
./security/zap/run-zap-api.ps1 -TargetUrl "https://<host>/api/"
```

## Outputs

- Baseline:
  - `security/zap/results/zap-baseline-<timestamp>.html`
  - `security/zap/results/zap-baseline-<timestamp>.json`
  - `security/zap/results/zap-baseline-<timestamp>.md`
- API:
  - `security/zap/results/zap-api-<timestamp>.html`
  - `security/zap/results/zap-api-<timestamp>.json`
  - `security/zap/results/zap-api-<timestamp>.md`

## Notes on scan coverage

- If endpoints require auth (e.g., `/api` returns 401), unauthenticated scanning will be limited.
- If the Swagger/OpenAPI spec is reachable, ZAP can still enumerate endpoints from the spec.

## Example results (Jan 8, 2026)

- Baseline: 0 FAIL, 9 WARN (mostly missing security headers / header hardening)
- API: 0 FAIL, 8 WARN (mostly security headers on Swagger/OpenAPI endpoints)
