# OWASP ZAP (Baseline)

This folder contains a minimal, repeatable ZAP scan runner for GymHive.

## What it does

- Runs an **unauthenticated OWASP ZAP baseline scan** against the GymHive ingress URL.
- Produces HTML + JSON + Markdown reports under `security/zap/results/`.

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
