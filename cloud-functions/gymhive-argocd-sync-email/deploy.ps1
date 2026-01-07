param(
  [Parameter(Mandatory = $false)]
  [string]$ProjectId = "gymhive-483522",

  [Parameter(Mandatory = $false)]
  [string]$Region = "europe-west1",

  [Parameter(Mandatory = $false)]
  [string]$FunctionName = "gymhive-argocd-sync-email",

  # Deprecated: the function now reads SENDGRID_API_KEY from Secret Manager via --set-secrets.
  # Kept for backward-compat so existing scripts don't break.
  [Parameter(Mandatory = $false)]
  [string]$SendGridApiKey,

  [Parameter(Mandatory = $true)]
  [string]$FromEmail,

  [Parameter(Mandatory = $false)]
  [string]$ToEmail = "ivan.p.petrov02@gmail.com",

  [Parameter(Mandatory = $true)]
  [string]$AdminEmailsUrl,

  [Parameter(Mandatory = $true)]
  [string]$AdminEmailsToken,

  [Parameter(Mandatory = $true)]
  [string]$WebhookToken

  ,
  [Parameter(Mandatory = $false)]
  [string]$SendGridTemplateId = ""

  ,
  [Parameter(Mandatory = $false)]
  [string]$IncludeRawPayload = "false"
)

$ErrorActionPreference = "Stop"

if (-not [string]::IsNullOrWhiteSpace($SendGridApiKey)) {
  Write-Warning "-SendGridApiKey is no longer used; SENDGRID_API_KEY is bound from Secret Manager (sendgrid-api-key:latest)."
}

Write-Host "Deploying Cloud Function '$FunctionName' to project '$ProjectId' in region '$Region'..."

gcloud config set project $ProjectId

$envVars = @(
  "FROM_EMAIL=$FromEmail",
  "TO_EMAIL=$ToEmail",
  "WEBHOOK_TOKEN=$WebhookToken",
  "ADMIN_EMAILS_URL=$AdminEmailsUrl",
  "ADMIN_EMAILS_TOKEN=$AdminEmailsToken",
  "INCLUDE_RAW_PAYLOAD=$IncludeRawPayload"
)

if (-not [string]::IsNullOrWhiteSpace($SendGridTemplateId)) {
  $envVars += "SENDGRID_TEMPLATE_ID=$SendGridTemplateId"
}

$envVarsArg = ($envVars -join ",")

gcloud functions deploy $FunctionName `
  --gen2 `
  --region $Region `
  --runtime nodejs20 `
  --source "./cloud-functions/gymhive-argocd-sync-email" `
  --entry-point "argocdSyncEmail" `
  --trigger-http `
  --allow-unauthenticated `
  --set-env-vars $envVarsArg `
  --set-secrets "SENDGRID_API_KEY=sendgrid-api-key:latest"

Write-Host "Done. Function URL:" 
$uri = gcloud functions describe $FunctionName --gen2 --region $Region --format="value(serviceConfig.uri)"
Write-Host $uri
