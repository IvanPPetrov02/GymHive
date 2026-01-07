param(
  [string]$ProjectId = "gymhive-483522",
  [string]$Namespace = "gymhive"
)

# This script syncs secrets from Google Secret Manager to Kubernetes.
# NOTE: If you are using External Secrets Operator (see external-secrets.yaml), you typically DO NOT need this script.
# Use it only if you want to create Kubernetes Secrets directly from Secret Manager values.

Write-Host "Syncing secrets from Google Secret Manager to Kubernetes..." -ForegroundColor Cyan
Write-Host "Project: $ProjectId" -ForegroundColor DarkGray
Write-Host "Namespace: $Namespace" -ForegroundColor DarkGray

try {
  gcloud --version 2>&1 | Out-Null
} catch {
  Write-Host "gcloud CLI is not installed or not on PATH." -ForegroundColor Red
  exit 1
}

try {
  kubectl version --client --short 2>&1 | Out-Null
} catch {
  Write-Host "kubectl is not installed or not on PATH." -ForegroundColor Red
  exit 1
}

function Get-GcpSecretValue([string]$SecretName) {
  return (gcloud secrets versions access latest --project="$ProjectId" --secret="$SecretName")
}

# Fetch secrets from Google Secret Manager
$jwtSecret = Get-GcpSecretValue "jwt-secret"
$passwordPepper = Get-GcpSecretValue "password-pepper"
$rabbitmqPassword = Get-GcpSecretValue "rabbitmq-password"
$mysqlPassword = Get-GcpSecretValue "mysql-root-password"
$mongodbConnection = Get-GcpSecretValue "mongodb-connection-string"

$authDbConnection = Get-GcpSecretValue "auth-db-connection-string"
$gymDbConnection = Get-GcpSecretValue "gym-db-connection-string"
$notificationsDbConnection = Get-GcpSecretValue "notifications-db-connection-string"
$workoutDbConnection = Get-GcpSecretValue "workout-db-connection-string"

Write-Host "Creating/updating Kubernetes Secrets..." -ForegroundColor Yellow

# JWT secret
kubectl delete secret gymhive-jwt-secret -n $Namespace --ignore-not-found=true | Out-Null
kubectl create secret generic gymhive-jwt-secret -n $Namespace `
  --from-literal=JWT_SECRET="$jwtSecret" | Out-Null

# Password pepper
kubectl delete secret gymhive-password-pepper -n $Namespace --ignore-not-found=true | Out-Null
kubectl create secret generic gymhive-password-pepper -n $Namespace `
  --from-literal=PASSWORD_PEPPER="$passwordPepper" | Out-Null

# MySQL root password (also used by MongoDB in these manifests)
kubectl delete secret gymhive-mysql-password -n $Namespace --ignore-not-found=true | Out-Null
kubectl create secret generic gymhive-mysql-password -n $Namespace `
  --from-literal=MYSQL_ROOT_PASSWORD="$mysqlPassword" | Out-Null

# RabbitMQ password
kubectl delete secret gymhive-rabbitmq-password -n $Namespace --ignore-not-found=true | Out-Null
kubectl create secret generic gymhive-rabbitmq-password -n $Namespace `
  --from-literal=RABBITMQ_PASSWORD="$rabbitmqPassword" | Out-Null

# MongoDB connection string
kubectl delete secret gymhive-mongodb-connection -n $Namespace --ignore-not-found=true | Out-Null
kubectl create secret generic gymhive-mongodb-connection -n $Namespace `
  --from-literal=MONGODB_CONNECTION_STRING="$mongodbConnection" | Out-Null

# Per-service DB connection strings
kubectl delete secret gymhive-auth-connection -n $Namespace --ignore-not-found=true | Out-Null
kubectl create secret generic gymhive-auth-connection -n $Namespace `
  --from-literal=CONNECTION_STRING="$authDbConnection" | Out-Null

kubectl delete secret gymhive-gym-connection -n $Namespace --ignore-not-found=true | Out-Null
kubectl create secret generic gymhive-gym-connection -n $Namespace `
  --from-literal=CONNECTION_STRING="$gymDbConnection" | Out-Null

kubectl delete secret gymhive-notifications-connection -n $Namespace --ignore-not-found=true | Out-Null
kubectl create secret generic gymhive-notifications-connection -n $Namespace `
  --from-literal=CONNECTION_STRING="$notificationsDbConnection" | Out-Null

kubectl delete secret gymhive-workout-connection -n $Namespace --ignore-not-found=true | Out-Null
kubectl create secret generic gymhive-workout-connection -n $Namespace `
  --from-literal=CONNECTION_STRING="$workoutDbConnection" | Out-Null

Write-Host "Secrets synced successfully!" -ForegroundColor Green
