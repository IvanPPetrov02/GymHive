# GymHive Cloud Function: Health Check

This Cloud Function checks your public GymHive endpoints (via the HTTPS ingress) and writes a JSON summary to Cloud Logging.

## What it checks

Default paths:
- `/`
- `/api/health`
- `/api/auth/health`
- `/api/gyms/health`
- `/api/memberships/health`
- `/api/notifications/health`
- `/api/workouts/health`

## Environment variables

- `BASE_URL` (required)
  - Example: `https://gymhive.34.8.235.214.nip.io`
- `TIMEOUT_MS` (optional)
  - Default: `5000`
- `ENDPOINTS` (optional)
  - Comma-separated list of paths to check.

## Deploy (GCP Cloud Functions Gen2)

From the repo root:

```powershell
$PROJECT_ID = "gymhive-483522"
$REGION = "europe-west1"
$FUNCTION_NAME = "gymhive-health-check"
$BASE_URL = "https://gymhive.34.8.235.214.nip.io"

gcloud config set project $PROJECT_ID

gcloud functions deploy $FUNCTION_NAME `
  --gen2 `
  --region $REGION `
  --runtime nodejs20 `
  --source "./cloud-functions/gymhive-health-check" `
  --entry-point "gymhiveHealthCheck" `
  --trigger-http `
  --allow-unauthenticated `
  --set-env-vars "BASE_URL=$BASE_URL,TIMEOUT_MS=5000"
```

## Create a Scheduler job (optional)

This runs the function every 5 minutes:

```powershell
$REGION = "europe-west1"
$FUNCTION_NAME = "gymhive-health-check"
$JOB_NAME = "gymhive-health-check-every-5m"

# Get function URL
$FUNCTION_URL = gcloud functions describe $FUNCTION_NAME --gen2 --region $REGION --format="value(serviceConfig.uri)"

gcloud scheduler jobs create http $JOB_NAME `
  --location $REGION `
  --schedule "*/5 * * * *" `
  --uri $FUNCTION_URL `
  --http-method GET
```

If you want this job to be authenticated (recommended), we can switch to OIDC auth with a service account and remove `--allow-unauthenticated`.
