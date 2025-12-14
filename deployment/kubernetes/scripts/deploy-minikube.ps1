param(
  [string]$EnvFile = (Join-Path $PSScriptRoot "..\..\..\.env"),
  [string]$Namespace = "gymhive"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Read-DotEnv([string]$Path) {
  if (-not (Test-Path -LiteralPath $Path)) {
    throw "Env file not found: $Path"
  }

  $map = @{}
  foreach ($line in Get-Content -LiteralPath $Path) {
    $trim = $line.Trim()
    if ($trim.Length -eq 0) { continue }
    if ($trim.StartsWith('#')) { continue }

    $idx = $trim.IndexOf('=')
    if ($idx -lt 1) { continue }

    $key = $trim.Substring(0, $idx).Trim()
    $value = $trim.Substring($idx + 1).Trim()

    if (($value.StartsWith('"') -and $value.EndsWith('"')) -or ($value.StartsWith("'") -and $value.EndsWith("'"))) {
      $value = $value.Substring(1, $value.Length - 2)
    }

    if ($key.Length -gt 0) {
      $map[$key] = $value
    }
  }
  return $map
}

function New-RandomSecret([int]$Bytes = 32) {
  $buffer = New-Object byte[] $Bytes
  $rng = [System.Security.Cryptography.RandomNumberGenerator]::Create()
  try {
    $rng.GetBytes($buffer)
  } finally {
    $rng.Dispose()
  }
  $base64 = [Convert]::ToBase64String($buffer)
  $base64 = $base64.TrimEnd('=')
  if ($base64.Length -gt 48) { return $base64.Substring(0, 48) }
  return $base64
}

function Apply-Secret([string]$Name, [hashtable]$Data, [string]$Ns) {
  $args = @("create", "secret", "generic", $Name, "-n", $Ns, "--dry-run=client", "-o", "yaml")
  foreach ($key in $Data.Keys) {
    $args += "--from-literal=$key=$($Data[$key])"
  }

  $yaml = & kubectl @args
  if ($LASTEXITCODE -ne 0) { throw "Failed to generate secret manifest for $Name" }

  $yaml | & kubectl apply -f - | Out-Null
  if ($LASTEXITCODE -ne 0) { throw "Failed to apply secret $Name" }
}

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..\..")
$envMap = Read-DotEnv $EnvFile

$required = @(
  "JWT_SECRET",
  "PASSWORD_PEPPER",
  "RABBITMQ_PASSWORD"
)

$missing = @($required | Where-Object { -not $envMap.ContainsKey($_) -or [string]::IsNullOrWhiteSpace($envMap[$_]) })
if ($missing.Count -gt 0) {
  throw "Missing required keys in .env: $($missing -join ', ')"
}

# Prefer a single MySQL root password if provided; otherwise fall back to per-service DB passwords;
# if still empty, generate one for the local cluster and keep it only in Kubernetes Secret.
$mysqlRoot = $null
foreach ($candidate in @(
  "MYSQL_ROOT_PASSWORD",
  "AUTH_DB_PASSWORD",
  "GYM_DB_PASSWORD",
  "NOTIFICATIONS_DB_PASSWORD",
  "WORKOUT_DB_PASSWORD"
)) {
  if ($envMap.ContainsKey($candidate) -and -not [string]::IsNullOrWhiteSpace($envMap[$candidate])) {
    $mysqlRoot = $envMap[$candidate]
    break
  }
}

if ([string]::IsNullOrWhiteSpace($mysqlRoot)) {
  $mysqlRoot = New-RandomSecret
  Write-Host "MYSQL root password not found in .env; generated one for minikube and stored it only in Kubernetes Secret (not in YAML)."
}

Write-Host "Starting minikube (docker driver)..."

# Ensure Docker engine is reachable (minikube docker driver requirement)
& docker version *> $null
if ($LASTEXITCODE -ne 0) {
  throw "Docker engine not reachable. Start Docker Desktop (WSL2 backend) and retry. You should be able to run: docker version"
}

& minikube start --driver=docker | Out-Host
if ($LASTEXITCODE -ne 0) {
  throw "minikube start failed. Fix the minikube driver prerequisites (usually Docker Desktop not running) and retry."
}

Write-Host "Switching kubectl context to minikube..."
& kubectl config use-context minikube | Out-Null

Write-Host "Creating namespace '$Namespace' (if needed)..."
& kubectl get namespace $Namespace 2>$null | Out-Null
if ($LASTEXITCODE -ne 0) {
  & kubectl create namespace $Namespace | Out-Null
}

Write-Host "Applying non-sensitive config..."
& kubectl apply -f (Join-Path $repoRoot "deployment\kubernetes\secure-config.yaml") | Out-Null

# Ensure local (minikube) uses in-cluster hostnames and dev-friendly environment.
& kubectl patch configmap gymhive-config -n $Namespace --type merge -p '{"data": {"ASPNETCORE_ENVIRONMENT": "Development", "ASPNETCORE_URLS": "http://+:8080", "RABBITMQ_HOST": "rabbitmq", "AUTH_DB_SERVER": "auth-db", "GYM_DB_SERVER": "gym-db", "NOTIFICATIONS_DB_SERVER": "notifications-db", "WORKOUT_DB_SERVER": "workout-db"}}' | Out-Null

Write-Host "Creating secrets from .env (no secrets in YAML)..."
Apply-Secret -Name "gymhive-jwt-secret" -Ns $Namespace -Data @{ JWT_SECRET = $envMap["JWT_SECRET"] }
Apply-Secret -Name "gymhive-password-pepper" -Ns $Namespace -Data @{ PASSWORD_PEPPER = $envMap["PASSWORD_PEPPER"] }
Apply-Secret -Name "gymhive-mysql-password" -Ns $Namespace -Data @{ MYSQL_ROOT_PASSWORD = $mysqlRoot }
Apply-Secret -Name "gymhive-rabbitmq-password" -Ns $Namespace -Data @{ RABBITMQ_PASSWORD = $envMap["RABBITMQ_PASSWORD"] }

# Use in-cluster MongoDB hostname for minikube. (No auth by default in our mongodb.yaml.)
Apply-Secret -Name "gymhive-mongodb-connection" -Ns $Namespace -Data @{ MONGODB_CONNECTION_STRING = "mongodb://mongodb:27017" }

$authConn = "Server=auth-db;Port=3306;Database=GymHive;User=root;Password=$mysqlRoot;"
$gymConn = "Server=gym-db;Port=3306;Database=GymHiveGyms;User=root;Password=$mysqlRoot;"
$notificationsConn = "Server=notifications-db;Port=3306;Database=GymHiveNotifications;User=root;Password=$mysqlRoot;"
$workoutConn = "Server=workout-db;Port=3306;Database=GymHiveWorkoutLogs;User=root;Password=$mysqlRoot;"

Apply-Secret -Name "gymhive-auth-connection" -Ns $Namespace -Data @{ CONNECTION_STRING = $authConn }
Apply-Secret -Name "gymhive-gym-connection" -Ns $Namespace -Data @{ CONNECTION_STRING = $gymConn }
Apply-Secret -Name "gymhive-notifications-connection" -Ns $Namespace -Data @{ CONNECTION_STRING = $notificationsConn }
Apply-Secret -Name "gymhive-workout-connection" -Ns $Namespace -Data @{ CONNECTION_STRING = $workoutConn }

Write-Host "Deploying databases..."
& kubectl apply -f (Join-Path $repoRoot "deployment\kubernetes\databases") | Out-Null

Write-Host "Deploying services..."
& kubectl apply -f (Join-Path $repoRoot "deployment\kubernetes\services") | Out-Null

Write-Host "Waiting briefly, then showing pod status..."
Start-Sleep -Seconds 10
& kubectl get pods -n $Namespace | Out-Host

Write-Host ""
Write-Host "Local access (recommended on Windows):"
Write-Host "- In one terminal: kubectl port-forward -n $Namespace svc/api-gateway 5000:80"
Write-Host "- In another terminal: kubectl port-forward -n $Namespace svc/frontend 5173:80"
Write-Host "- Then open: http://localhost:5173 (frontend) and it will call http://localhost:5000 (API)"
