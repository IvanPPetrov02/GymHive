# This script syncs secrets from Google Secret Manager to Kubernetes
# Run this script whenever you update secrets in Google Secret Manager

Write-Host "Syncing secrets from Google Secret Manager to Kubernetes..." -ForegroundColor Cyan

# Get secrets from Google Secret Manager and create Kubernetes secret
$jwtSecret = gcloud secrets versions access latest --secret="jwt-secret"
$passwordPepper = gcloud secrets versions access latest --secret="password-pepper"
$rabbitmqPassword = gcloud secrets versions access latest --secret="rabbitmq-password"
$mysqlPassword = gcloud secrets versions access latest --secret="mysql-root-password"
$mongodbConnection = gcloud secrets versions access latest --secret="mongodb-connection-string"

# Delete existing secret if it exists
kubectl delete secret gymhive-secrets -n gymhive --ignore-not-found=true

# Create new Kubernetes secret
kubectl create secret generic gymhive-secrets -n gymhive `
  --from-literal=JWT_SECRET="$jwtSecret" `
  --from-literal=PASSWORD_PEPPER="$passwordPepper" `
  --from-literal=RABBITMQ_PASSWORD="$rabbitmqPassword" `
  --from-literal=MYSQL_ROOT_PASSWORD="$mysqlPassword" `
  --from-literal=MONGODB_CONNECTION_STRING="$mongodbConnection"

Write-Host "Secrets synced successfully!" -ForegroundColor Green
Write-Host "All secrets are now stored in Google Secret Manager and synced to Kubernetes" -ForegroundColor Yellow
