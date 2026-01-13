# GymHive — ArgoCD Sync → Email (Cloud Function Gen2 / Cloud Run)

This folder contains the Node.js (20) Cloud Function (Gen2) that receives an ArgoCD Notifications webhook and sends a “what changed” email to all GymHive admin users via SendGrid.

Gen2 Cloud Functions run on Cloud Run infrastructure (function URL looks like `https://<service>-<hash>-<region>.a.run.app`).

## What was done

- Implemented an HTTP handler (`argocdSyncEmail`) for ArgoCD Notifications.
- Added shared-secret validation (ArgoCD → function).
- Added recipient resolution via GymHive admin-emails endpoint (function → GymHive, shared-secret).
- Added SendGrid delivery (direct HTML+text or optional Dynamic Template).

## End-to-end flow

1. ArgoCD sync succeeds.
2. ArgoCD Notifications POSTs a webhook payload to the function.
3. Function validates `X-GymHive-Webhook-Token` against `WEBHOOK_TOKEN`.
4. Function fetches `{ emails: [...] }` from `ADMIN_EMAILS_URL` using `X-GymHive-AdminEmails-Token`.
5. Function sends an email via SendGrid.

## Files

- `index.js` — implementation (exports `argocdSyncEmail`)
- `deploy.ps1` — deploy helper
- `package.json` — dependency list

## Configuration

Required env vars:

- `FROM_EMAIL` (verified sender in SendGrid)
- `WEBHOOK_TOKEN`
- `ADMIN_EMAILS_URL`
- `ADMIN_EMAILS_TOKEN`

Secret Manager binding (deploy script):

- `SENDGRID_API_KEY` (bound from Secret Manager)

Optional env vars:

- `TO_EMAIL` (fallback recipient)
- `SENDGRID_TEMPLATE_ID` (SendGrid Dynamic Template ID)
- `INCLUDE_RAW_PAYLOAD` (`true` includes truncated JSON payload)
- `DEBUG_AUTH` (`true` returns safe 401 diagnostics: token lengths only)

## Key snippets

### Webhook auth (ArgoCD → function)

```js
const token = (req.get("X-GymHive-Webhook-Token") || "").trim();
const expectedToken = requiredEnv("WEBHOOK_TOKEN").trim();

if (!token || token !== expectedToken) {
  const debugAuth = (process.env.DEBUG_AUTH || "").trim().toLowerCase() === "true";
  res.status(401).json({
    status: "error",
    message: "Unauthorized",
    ...(debugAuth
      ? {
          receivedTokenPresent: Boolean(token),
          receivedTokenLength: token.length,
          expectedTokenLength: expectedToken.length
        }
      : {})
  });
  return;
}
```

### Resolve recipients (function → GymHive)

```js
const response = await fetch(requiredEnv("ADMIN_EMAILS_URL"), {
  method: "GET",
  headers: {
    "X-GymHive-AdminEmails-Token": requiredEnv("ADMIN_EMAILS_TOKEN")
  }
});

const data = await response.json();
const recipients = (Array.isArray(data?.emails) ? data.emails : [])
  .map((e) => String(e || "").trim())
  .filter(Boolean);
```

Expected response:

```json
{ "emails": ["admin1@example.com", "admin2@example.com"] }
```

### Send email via SendGrid

```js
sgMail.setApiKey(requiredEnv("SENDGRID_API_KEY"));

const subject = `[GymHive][ArgoCD] Synced: ${summary.appName} @ ${String(summary.revision).slice(0, 12)}`;
const templateId = (process.env.SENDGRID_TEMPLATE_ID || "").trim();

if (templateId) {
  await sgMail.send({
    to: recipients,
    from: requiredEnv("FROM_EMAIL"),
    subject,
    templateId,
    dynamicTemplateData: {
      appName: summary.appName,
      revision: summary.revision,
      repoURL: summary.repoURL,
      changedResources: content.resources,
      changedCount: content.resources.length
    }
  });
} else {
  await sgMail.send({
    to: recipients,
    from: requiredEnv("FROM_EMAIL"),
    subject,
    text: content.text,
    html: content.html
  });
}
```

## Deploy (Windows / PowerShell)

Create Secret Manager secret (one-time):

```powershell
gcloud secrets create sendgrid-api-key --replication-policy=automatic
"SG.xxxxx" | gcloud secrets versions add sendgrid-api-key --data-file=-
```

Deploy:

```powershell
./cloud-functions/gymhive-argocd-sync-email/deploy.ps1 `
  -ProjectId "<gcp-project-id>" `
  -Region "<region>" `
  -FunctionName "gymhive-argocd-sync-email" `
  -FromEmail "info.gymhive@gmail.com" `
  -ToEmail "fallback@example.com" `
  -AdminEmailsUrl "https://<ingress>/api/auth/admin-emails" `
  -AdminEmailsToken "<ADMIN_EMAILS_TOKEN>" `
  -WebhookToken "<WEBHOOK_TOKEN>"
```

Secret binding excerpt:

```powershell
"--set-secrets", "SENDGRID_API_KEY=sendgrid-api-key:latest"
```

## ArgoCD Notifications header substitution

When configuring ArgoCD Notifications, the header value should be set using Notifications secret substitution:

```yaml
headers:
  - name: X-GymHive-Webhook-Token
    value: $gymhiveWebhookToken
```
