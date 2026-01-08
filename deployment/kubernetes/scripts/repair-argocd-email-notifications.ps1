param(
  [Parameter(Mandatory = $false)]
  [string]$ProjectId = "gymhive-483522",

  [Parameter(Mandatory = $false)]
  [string]$Region = "europe-west1",

  [Parameter(Mandatory = $false)]
  [string]$FunctionName = "gymhive-argocd-sync-email",

  [Parameter(Mandatory = $false)]
  [string]$FromEmail = "info.gymhive@gmail.com",

  [Parameter(Mandatory = $false)]
  [string]$AppName = "gymhive-app",

  [Parameter(Mandatory = $false)]
  [string]$ArgoNamespace = "argocd",

  [Parameter(Mandatory = $false)]
  [string]$GymHiveNamespace = "gymhive"
)

$ErrorActionPreference = "Stop"

function New-RandomToken {
  $chars = (48..57) + (65..90) + (97..122)
  -join (Get-Random -Count 40 -InputObject $chars | ForEach-Object { [char]$_ })
}

function Assert-LastExitCode {
  param([string]$Message)
  if ($LASTEXITCODE -ne 0) {
    throw $Message
  }
}

# Repo root = 3 levels above this script
$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..\..")
Set-Location $repoRoot

Write-Host "Repairing ArgoCD -> Cloud Function -> SendGrid notifications (no secrets printed)."

# 1) Compute ADMIN_EMAILS_URL from ingress host
$ingressJson = kubectl -n $GymHiveNamespace get ingress gymhive-ingress -o json | ConvertFrom-Json
$ingressHost = $ingressJson.spec.rules[0].host
if ([string]::IsNullOrWhiteSpace($ingressHost)) {
  throw "Could not determine ingress host from gymhive-ingress."
}
$adminEmailsUrl = "https://$ingressHost/api/auth/admin-emails"

# 2) Generate fresh tokens
$webhookToken = New-RandomToken
$adminEmailsToken = New-RandomToken


# 3) Rotate admin-emails token in Secret Manager (source of truth)
gcloud.cmd config set project $ProjectId | Out-Null
$tokenFile = [System.IO.Path]::GetTempFileName()
try {
  Set-Content -Path $tokenFile -Value $adminEmailsToken -NoNewline
  gcloud.cmd secrets versions add admin-emails-token --data-file=$tokenFile | Out-Null
  Assert-LastExitCode "Failed to add new version to Secret Manager secret 'admin-emails-token'."
}
finally {
  Remove-Item -Path $tokenFile -Force -ErrorAction SilentlyContinue
}

# 4) Patch in-cluster secret immediately and restart auth-service
$adminTokenB64 = [Convert]::ToBase64String([Text.Encoding]::UTF8.GetBytes($adminEmailsToken))
$secretPatch = @{ data = @{ ADMIN_EMAILS_TOKEN = $adminTokenB64 } } | ConvertTo-Json -Compress
$patchFile = [System.IO.Path]::GetTempFileName()
try {
  Set-Content -Path $patchFile -Value $secretPatch -NoNewline
  kubectl -n $GymHiveNamespace patch secret gymhive-admin-emails-token --type merge --patch-file $patchFile | Out-Null
}
finally {
  Remove-Item -Path $patchFile -Force -ErrorAction SilentlyContinue
}

kubectl -n $GymHiveNamespace rollout restart deployment auth-service | Out-Null

# 5) Fetch Cloud Function URL (for notifications config)
$functionUrl = (gcloud.cmd functions describe $FunctionName --gen2 --region $Region --format="value(serviceConfig.uri)")
if ([string]::IsNullOrWhiteSpace($functionUrl)) {
  throw "Could not resolve Cloud Function URL for '$FunctionName'."
}

# 6) Apply ArgoCD Notifications config with rotated webhook token
$setupScript = Join-Path $repoRoot "deployment\kubernetes\scripts\setup-argocd-email-webhook.ps1"
& $setupScript -FunctionUrl $functionUrl -WebhookToken $webhookToken | Out-Null
kubectl -n $ArgoNamespace rollout restart deployment argocd-notifications-controller | Out-Null

# 7) Redeploy Cloud Function with the matching tokens + correct ADMIN_EMAILS_URL
$deployScript = Join-Path $repoRoot "cloud-functions\gymhive-argocd-sync-email\deploy.ps1"
& $deployScript -ProjectId $ProjectId -Region $Region -FunctionName $FunctionName -FromEmail $FromEmail -AdminEmailsUrl $adminEmailsUrl -AdminEmailsToken $adminEmailsToken -WebhookToken $webhookToken | Out-Null

# 8) Trigger an ArgoCD sync (so we get a notification event)
$syncPatchObj = @{ operation = @{ sync = @{ revision = ""; prune = $false } } }
$syncPatch = $syncPatchObj | ConvertTo-Json -Compress
$syncPatchFile = [System.IO.Path]::GetTempFileName()
try {
  Set-Content -Path $syncPatchFile -Value $syncPatch -NoNewline
  kubectl -n $ArgoNamespace patch application $AppName --type merge --patch-file $syncPatchFile | Out-Null
  if ($LASTEXITCODE -ne 0) {
    throw "Failed to trigger ArgoCD sync via kubectl patch."
  }
}
finally {
  Remove-Item -Path $syncPatchFile -Force -ErrorAction SilentlyContinue
}

Write-Host "Done. Tokens rotated, configs applied, function deployed, sync triggered."