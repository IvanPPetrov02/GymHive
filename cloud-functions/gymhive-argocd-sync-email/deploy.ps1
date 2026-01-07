param(
  [Parameter(Mandatory = $false)]
  [string]$ProjectId = "gymhive-483522",

  [Parameter(Mandatory = $false)]
  [string]$Region = "europe-west1",

  [Parameter(Mandatory = $false)]
  [string]$FunctionName = "gymhive-argocd-sync-email",

  [Parameter(Mandatory = $true)]
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
)

$ErrorActionPreference = "Stop"

Write-Host "Deploying Cloud Function '$FunctionName' to project '$ProjectId' in region '$Region'..."

gcloud config set project $ProjectId

gcloud functions deploy $FunctionName `
  --gen2 `
  --region $Region `
  --runtime nodejs20 `
  --source "./cloud-functions/gymhive-argocd-sync-email" `
  --entry-point "argocdSyncEmail" `
  --trigger-http `
  --allow-unauthenticated `
  --set-env-vars "SENDGRID_API_KEY=$SendGridApiKey,FROM_EMAIL=$FromEmail,TO_EMAIL=$ToEmail,WEBHOOK_TOKEN=$WebhookToken,ADMIN_EMAILS_URL=$AdminEmailsUrl,ADMIN_EMAILS_TOKEN=$AdminEmailsToken"

Write-Host "Done. Function URL:" 
$uri = gcloud functions describe $FunctionName --gen2 --region $Region --format="value(serviceConfig.uri)"
Write-Host $uri
