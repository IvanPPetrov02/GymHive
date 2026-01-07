param(
  [Parameter(Mandatory = $false)]
  [string]$ProjectId = "gymhive-483522",

  [Parameter(Mandatory = $false)]
  [string]$Region = "europe-west1",

  [Parameter(Mandatory = $false)]
  [string]$FunctionName = "gymhive-health-check",

  [Parameter(Mandatory = $false)]
  [string]$BaseUrl = "https://gymhive.34.8.235.214.nip.io",

  [Parameter(Mandatory = $false)]
  [int]$TimeoutMs = 5000
)

$ErrorActionPreference = "Stop"

Write-Host "Deploying Cloud Function '$FunctionName' to project '$ProjectId' in region '$Region'..."

gcloud config set project $ProjectId

gcloud functions deploy $FunctionName `
  --gen2 `
  --region $Region `
  --runtime nodejs20 `
  --source "./cloud-functions/gymhive-health-check" `
  --entry-point "gymhiveHealthCheck" `
  --trigger-http `
  --allow-unauthenticated `
  --set-env-vars "BASE_URL=$BaseUrl,TIMEOUT_MS=$TimeoutMs"

Write-Host "Done. Function URL:" 
$uri = gcloud functions describe $FunctionName --gen2 --region $Region --format="value(serviceConfig.uri)"
Write-Host $uri
