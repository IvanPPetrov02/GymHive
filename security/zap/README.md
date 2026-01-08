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
