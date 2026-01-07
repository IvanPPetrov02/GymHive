# GymHive Cloud Function: ArgoCD Sync → Email

This Cloud Function receives a webhook from **ArgoCD Notifications**, fetches the current **admin user emails** from GymHive, and sends a clean “what changed” email using **SendGrid**.

## What you get

- When ArgoCD successfully syncs the `gymhive-app` Application, an HTTP webhook fires.
- The Cloud Function validates a shared-secret header.
- It calls GymHive’s admin-emails endpoint (also protected by a shared-secret header).
- It sends one email to all admin recipients (dynamic list).

## Architecture (end-to-end)

Add your own diagram/screenshot here:

![Architecture diagram placeholder](./images/architecture-placeholder.png)

High-level flow:

1. ArgoCD sync succeeds.
2. ArgoCD Notifications sends a webhook to the Cloud Function.
3. Cloud Function validates `X-GymHive-Webhook-Token`.
4. Cloud Function calls GymHive `ADMIN_EMAILS_URL` with `X-GymHive-AdminEmails-Token`.
5. Cloud Function sends email via SendGrid.

## Why this is a “real Cloud Function”

- The code lives in this repo and is deployed with `gcloud functions deploy --gen2`.
- Gen2 Cloud Functions run on Cloud Run infrastructure (your function URL will look like `https://<service>-<hash>-<region>.a.run.app`).
- Scaling, HTTPS, and runtime provisioning are managed for you.

## Folder layout

Add a screenshot of the folder in VS Code if you want:

![Folder layout placeholder](./images/folder-layout-placeholder.png)

Key files:

- `index.js`: HTTP handler `argocdSyncEmail` (the Cloud Function entrypoint)
- `deploy.ps1`: deployment helper for Windows/PowerShell
- `package.json`: Node.js dependencies (SendGrid client, etc.)

## Security model (important)

This feature intentionally avoids end-user JWT auth for machine-to-machine calls and uses **two shared secrets**:

1) **Webhook secret** (ArgoCD → function)

- Header: `X-GymHive-Webhook-Token`
- Env var: `WEBHOOK_TOKEN`
- If missing or wrong → the function returns `401`.

2) **Admin emails secret** (function → GymHive)

- Header: `X-GymHive-AdminEmails-Token`
- Env var: `ADMIN_EMAILS_TOKEN`
- Sent to `ADMIN_EMAILS_URL`.

This means:

- The function can be deployed `--allow-unauthenticated` (it is still protected by the webhook token).
- GymHive can expose a special endpoint that is not user-authenticated, but still locked by a shared secret.

## Environment variables

Required:

- `FROM_EMAIL` (SendGrid verified sender, e.g. `info.gymhive@gmail.com`)
- `WEBHOOK_TOKEN`
- `ADMIN_EMAILS_URL` (e.g. `https://gymhive.<IP>.nip.io/api/auth/admin-emails`)
- `ADMIN_EMAILS_TOKEN`

Optional:

- `TO_EMAIL` (fallback if admin lookup fails; default is hardcoded in code)
- `SENDGRID_TEMPLATE_ID` (SendGrid Dynamic Template ID)
- `INCLUDE_RAW_PAYLOAD` (default `false`; set `true` to include raw payload)

Secret Manager binding:

- `SENDGRID_API_KEY` is bound via Secret Manager in the deploy script (see below).

## How the function works (code-level)

### 1) Webhook request validation

The handler rejects requests if the webhook header doesn’t match:

```js
const token = (req.get("X-GymHive-Webhook-Token") || "").trim();
if (!token || token !== requiredEnv("WEBHOOK_TOKEN")) {
  res.status(401).json({ error: "Unauthorized" });
  return;
}
```

Add screenshot of a failing request in Postman/curl:

![401 placeholder](./images/webhook-401-placeholder.png)

### 2) Parse the ArgoCD Notifications payload

The webhook body comes from the ArgoCD Notifications template and typically looks like:

```json
{
  "app": { /* ArgoCD Application object */ },
  "context": { /* notification context */ }
}
```

The function extracts:

- App name
- Revision
- Sync/health status
- Resource list changes (Deployments, Services, etc.)

### 3) Resolve recipients (dynamic admin list)

The function calls `ADMIN_EMAILS_URL` with the shared-secret header:

```js
await fetch(adminEmailsUrl, {
  headers: {
    "X-GymHive-AdminEmails-Token": adminEmailsToken,
  },
});
```

Expected response:

```json
{ "emails": ["admin1@example.com", "admin2@example.com"] }
```

If this lookup fails, the function can fall back to `TO_EMAIL` (so you still get notifications during outages).

### 4) Build a readable email (HTML + text)

By default (no template ID), the function builds:

- A subject like: `GymHive: ArgoCD sync succeeded (gymhive-app)`
- A short HTML summary with a “changed resources” list
- A plain-text fallback body

Optional behavior:

- If `INCLUDE_RAW_PAYLOAD=true`, it appends a truncated JSON payload block.

### 5) Send via SendGrid

The function uses the SendGrid Node SDK with API key loaded from environment (`SENDGRID_API_KEY`).
In our setup, that env var is injected from **Secret Manager**.

## Optional: SendGrid Dynamic Template

If you create a template in SendGrid, set `SENDGRID_TEMPLATE_ID` at deploy time.
Then the function will use SendGrid template rendering via `dynamicTemplateData`.

Add your template screenshot here:

![SendGrid template placeholder](./images/sendgrid-template-placeholder.png)

## Deploy (Windows / PowerShell)

### 1) Prereqs

- `gcloud` installed and authenticated (`gcloud auth login`)
- Access to the GCP project
- A Secret Manager secret named `sendgrid-api-key`

Create/update the secret (only if you haven’t already):

```powershell
gcloud secrets create sendgrid-api-key --replication-policy=automatic
# then add a version by piping the key:
"SG.xxxxx" | gcloud secrets versions add sendgrid-api-key --data-file=-
```

### 2) Deploy the function

```powershell
$ProjectId = "gymhive-483522"
$Region = "europe-west1"
$FunctionName = "gymhive-argocd-sync-email"

$FromEmail = "info.gymhive@gmail.com"
$ToEmail = "ivan.p.petrov02@gmail.com"   # optional fallback
$WebhookToken = "<random-long-secret>"
$AdminEmailsUrl = "https://gymhive.34.8.235.214.nip.io/api/auth/admin-emails"
$AdminEmailsToken = "<random-long-secret>"

./cloud-functions/gymhive-argocd-sync-email/deploy.ps1 `
  -ProjectId $ProjectId `
  -Region $Region `
  -FunctionName $FunctionName `
  -FromEmail $FromEmail `
  -ToEmail $ToEmail `
  -AdminEmailsUrl $AdminEmailsUrl `
  -AdminEmailsToken $AdminEmailsToken `
  -WebhookToken $WebhookToken
```

The script prints the Gen2 function URL at the end.

## Test (manual webhook)

```powershell
$FunctionUrl = gcloud functions describe gymhive-argocd-sync-email --gen2 --region europe-west1 --format="value(serviceConfig.uri)"
$Token = "<same WEBHOOK_TOKEN>"

$body = @{
  appName = "gymhive-app"
  revision = "abc123"
  resources = @(
    @{ kind = "Deployment"; name = "api-gateway"; namespace = "gymhive"; status = "Synced" }
  )
} | ConvertTo-Json

curl.exe -s -X POST "$FunctionUrl" `
  -H "Content-Type: application/json" `
  -H "X-GymHive-Webhook-Token: $Token" `
  --data "$body"
```

Add screenshot of successful response/email here:

![Email example placeholder](./images/email-example-placeholder.png)

## Wire ArgoCD Notifications → Cloud Function

Use the provided helper script:

```powershell
./deployment/kubernetes/scripts/setup-argocd-email-webhook.ps1 -FunctionUrl $FunctionUrl
```

Notes:

- If you don’t pass `-WebhookToken`, the script will generate one and print it once.
- Deploy/redeploy the Cloud Function with the same token as `WEBHOOK_TOKEN`.

## Troubleshooting

### I get `401 Unauthorized` from the function

- Check `X-GymHive-Webhook-Token` header is present.
- Confirm it matches the deployed `WEBHOOK_TOKEN` env var.

### Function can’t fetch admin emails (401/403)

- Confirm `ADMIN_EMAILS_URL` is reachable from the internet.
- Confirm the API Gateway allows that path without Bearer JWT (it should still require `X-GymHive-AdminEmails-Token`).
- Confirm `ADMIN_EMAILS_TOKEN` matches what the Auth service expects.

### Email sends but formatting looks plain

- Add a SendGrid Dynamic Template and deploy with `SENDGRID_TEMPLATE_ID`.
- Or keep built-in HTML formatting (default).
