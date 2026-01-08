$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..\..")
Set-Location $repoRoot

# Compute ADMIN_EMAILS_URL from ingress
$ing = kubectl -n gymhive get ingress gymhive-ingress -o json | ConvertFrom-Json
$ingressHost = $ing.spec.rules[0].host
if ([string]::IsNullOrWhiteSpace($ingressHost)) {
  throw "Ingress host not found on gymhive-ingress."
}
$adminEmailsUrl = "https://$ingressHost/api/auth/admin-emails"

# Read tokens from existing cluster secrets (do not print)
$webhookB64 = kubectl -n argocd get secret argocd-notifications-secret -o jsonpath="{.data.gymhiveWebhookToken}"
$webhookToken = [Text.Encoding]::UTF8.GetString([Convert]::FromBase64String($webhookB64))

$adminB64 = kubectl -n gymhive get secret gymhive-admin-emails-token -o jsonpath="{.data.ADMIN_EMAILS_TOKEN}"
$adminEmailsToken = [Text.Encoding]::UTF8.GetString([Convert]::FromBase64String($adminB64))

# Redeploy Cloud Function to match current tokens
$deployScript = Join-Path $repoRoot "cloud-functions\gymhive-argocd-sync-email\deploy.ps1"
& $deployScript -ProjectId "gymhive-483522" -Region "europe-west1" -FunctionName "gymhive-argocd-sync-email" -FromEmail "info.gymhive@gmail.com" -AdminEmailsUrl $adminEmailsUrl -AdminEmailsToken $adminEmailsToken -WebhookToken $webhookToken -DebugAuth "true" | Out-Null

# Trigger sync via patch-file (avoids PowerShell quoting issues)
$syncPatchFile = [System.IO.Path]::GetTempFileName()
try {
  Set-Content -Path $syncPatchFile -Value '{"operation":{"sync":{"revision":"","prune":false}}}' -NoNewline
  kubectl -n argocd patch application gymhive-app --type merge --patch-file $syncPatchFile | Out-Null
}
finally {
  Remove-Item -Path $syncPatchFile -Force -ErrorAction SilentlyContinue
}

Write-Host "Cloud Function redeployed to match current cluster tokens; sync triggered."