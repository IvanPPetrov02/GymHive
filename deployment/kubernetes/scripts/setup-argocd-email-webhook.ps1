param(
  [Parameter(Mandatory = $true)]
  [string]$FunctionUrl,

  [Parameter(Mandatory = $false)]
  [string]$ApplicationName = "gymhive-app",

  [Parameter(Mandatory = $false)]
  [string]$ApplicationNamespace = "argocd",

  [Parameter(Mandatory = $false)]
  [string]$NotificationsNamespace = "argocd",

  [Parameter(Mandatory = $false)]
  [string]$WebhookToken
)

$ErrorActionPreference = "Stop"

function New-RandomToken {
  $chars = (48..57) + (65..90) + (97..122)
  -join (Get-Random -Count 40 -InputObject $chars | ForEach-Object { [char]$_ })
}

if (-not $WebhookToken) {
  $WebhookToken = New-RandomToken
  $generatedToken = $true
}

Write-Host "Configuring ArgoCD Notifications webhook service to call: $FunctionUrl"

# 1) Ensure notifications controller is running
kubectl -n $NotificationsNamespace scale deployment argocd-notifications-controller --replicas=1 | Out-Null

# 2) Store webhook token in argocd-notifications-secret
$tokenB64 = [Convert]::ToBase64String([Text.Encoding]::UTF8.GetBytes($WebhookToken))
$secretPatch = @{ data = @{ gymhiveWebhookToken = $tokenB64 } } | ConvertTo-Json -Compress
$tmpPatch = New-TemporaryFile
Set-Content -Path $tmpPatch.FullName -Value $secretPatch -NoNewline
kubectl -n $NotificationsNamespace patch secret argocd-notifications-secret --type merge --patch-file $tmpPatch.FullName | Out-Null
Remove-Item $tmpPatch.FullName -Force

# 3) Create/update argocd-notifications-cm with webhook service + trigger + template
# Notes:
# - We subscribe via an Application annotation (step 4).
# - Uses a shared-secret header: X-GymHive-Webhook-Token.
$cmYaml = @"
apiVersion: v1
kind: ConfigMap
metadata:
  name: argocd-notifications-cm
  namespace: $NotificationsNamespace
data:
  service.webhook.gymhive-email: |
    url: $FunctionUrl
    headers:
      - name: X-GymHive-Webhook-Token
        value: $gymhiveWebhookToken
    insecureSkipVerify: false

  template.gymhive-sync-email: |
    webhook:
      gymhive-email:
        method: POST
        path: /
        body: |
          {
            "app": {{ toJson .app }},
            "context": {{ toJson .context }}
          }

  trigger.on-sync-succeeded: |
    [{"description":"GymHive: ArgoCD sync succeeded","send":["gymhive-sync-email"],"when":"app.status.operationState.phase == 'Succeeded'"}]
"@

$cmYaml | kubectl apply -f - | Out-Null

# 4) Subscribe the ArgoCD Application to the trigger (webhook service name: gymhive-email)
$annotationKey = "notifications.argoproj.io/subscribe.on-sync-succeeded.gymhive-email"
kubectl -n $ApplicationNamespace annotate application $ApplicationName $annotationKey="" --overwrite | Out-Null

Write-Host "Done. Webhook token was generated/used but not printed."
if ($generatedToken) {
  Write-Host "Webhook token was generated and stored in the cluster secret."
} else {
  Write-Host "Webhook token was provided and stored in the cluster secret."
}
Write-Host "Next: deploy the Cloud Function with WEBHOOK_TOKEN set to the same value."