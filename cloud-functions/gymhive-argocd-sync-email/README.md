# GymHive Cloud Function: ArgoCD Sync → Email

This Cloud Function receives a webhook (from ArgoCD Notifications) and sends an email to the admin address describing what was updated.

## Why this counts as a Cloud Function
- The code lives in Git (this repo).
- When you deploy it with `gcloud functions deploy --gen2`, it runs serverlessly in GCP (like AWS Lambda / Azure Functions).

## Prereqs (SendGrid)

I chose **SendGrid** because it’s reliable and doesn’t require running your own SMTP server.

You will need:
- A SendGrid account
- A **verified sender** email (can be a Gmail address; no domain purchase required)
- A SendGrid API key

## Environment variables

- `SENDGRID_API_KEY` (required)
- `FROM_EMAIL` (required; must be a verified sender in SendGrid)
- `TO_EMAIL` (optional; default `ivan.p.petrov02@gmail.com`)
- `WEBHOOK_TOKEN` (required; webhook shared secret; checked against `X-GymHive-Webhook-Token`)

## Deploy

```powershell
$ProjectId = "gymhive-483522"
$Region = "europe-west1"
$FunctionName = "gymhive-argocd-sync-email"

# You must fill these in:
$SendGridApiKey = "SG.xxxxx"
$FromEmail = "ivan.p.petrov02@gmail.com"  # after verifying this sender in SendGrid
$ToEmail = "ivan.p.petrov02@gmail.com"
$WebhookToken = "<random-long-secret>"

./cloud-functions/gymhive-argocd-sync-email/deploy.ps1 `
  -ProjectId $ProjectId `
  -Region $Region `
  -FunctionName $FunctionName `
  -SendGridApiKey $SendGridApiKey `
  -FromEmail $FromEmail `
  -ToEmail $ToEmail `
  -WebhookToken $WebhookToken
```

## Test (manual)

```powershell
$FunctionUrl = "https://REGION-PROJECT.cloudfunctions.net/gymhive-argocd-sync-email"
$Token = "<same WEBHOOK_TOKEN>"

$body = @{ appName = "gymhive-app"; revision = "abc123"; resources = @(@{ kind = "Deployment"; name = "api-gateway"; namespace = "gymhive"; status = "Synced" }) } | ConvertTo-Json

curl.exe -s -X POST "$FunctionUrl" -H "Content-Type: application/json" -H "X-GymHive-Webhook-Token: $Token" --data "$body"
```

## Next step: wire ArgoCD Notifications

Once deployed, we’ll configure ArgoCD Notifications to call this function when sync succeeds.
