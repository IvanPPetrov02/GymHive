# Auto Port-Forward Script for GymHive Minikube
# This script automatically sets up port forwarding for frontend and API Gateway

Write-Host "========================================" -ForegroundColor Cyan
Write-Host " GymHive Port Forwarding Setup" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Check if minikube is running
try {
    $minikubeStatus = minikube status 2>&1
    if ($LASTEXITCODE -ne 0) {
        Write-Host "Error: Minikube is not running. Please start minikube first." -ForegroundColor Red
        exit 1
    }
} catch {
    Write-Host "Error: Minikube is not installed or not in PATH." -ForegroundColor Red
    exit 1
}

# Check if kubectl is configured
try {
    $context = kubectl config current-context 2>&1
    if ($context -notmatch "minikube") {
        Write-Host "Warning: Current kubectl context is not minikube!" -ForegroundColor Yellow
        Write-Host "Current context: $context" -ForegroundColor Yellow
        $continue = Read-Host "Continue anyway? (y/N)"
        if ($continue -ne "y") {
            exit 1
        }
    }
} catch {
    Write-Host "Error: kubectl is not installed or not configured." -ForegroundColor Red
    exit 1
}

# Check if gymhive namespace exists
try {
    kubectl get namespace gymhive 2>&1 | Out-Null
    if ($LASTEXITCODE -ne 0) {
        Write-Host "Error: gymhive namespace not found. Please deploy the application first." -ForegroundColor Red
        exit 1
    }
} catch {
    Write-Host "Error: Failed to check gymhive namespace." -ForegroundColor Red
    exit 1
}

Write-Host "Checking pod status..." -ForegroundColor Yellow
kubectl get pods -n gymhive

Write-Host ""
Write-Host "Starting port forwarding..." -ForegroundColor Green
Write-Host "  Frontend: localhost:3000 -> frontend:80" -ForegroundColor White
Write-Host "  API Gateway: localhost:5000 -> api-gateway:80" -ForegroundColor White
Write-Host ""

# Start port forwarding in separate windows
Start-Process powershell -ArgumentList "-NoExit", "-Command", "kubectl port-forward service/frontend 3000:80 -n gymhive"
Start-Sleep -Seconds 1

Start-Process powershell -ArgumentList "-NoExit", "-Command", "kubectl port-forward service/api-gateway 5000:80 -n gymhive"
Start-Sleep -Seconds 2

# Verify connections
Write-Host ""
Write-Host "Verifying connections..." -ForegroundColor Yellow

try {
    $frontendResponse = Invoke-WebRequest -Uri "http://localhost:3000" -TimeoutSec 5 -UseBasicParsing -ErrorAction SilentlyContinue
    if ($frontendResponse.StatusCode -eq 200) {
        Write-Host "  ✓ Frontend is accessible" -ForegroundColor Green
    }
} catch {
    Write-Host "  ✗ Frontend might not be ready yet" -ForegroundColor Red
}

try {
    $apiResponse = Invoke-WebRequest -Uri "http://localhost:5000/health" -TimeoutSec 5 -UseBasicParsing -ErrorAction SilentlyContinue
    if ($apiResponse.StatusCode -eq 200) {
        Write-Host "  ✓ API Gateway is accessible" -ForegroundColor Green
    }
} catch {
    Write-Host "  ✗ API Gateway might not be ready yet" -ForegroundColor Red
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Port forwarding is running!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Access your application at:" -ForegroundColor White
Write-Host "  Frontend:    http://localhost:3000" -ForegroundColor Cyan
Write-Host "  API Gateway: http://localhost:5000" -ForegroundColor Cyan
Write-Host ""
Write-Host "Two PowerShell windows have been opened for port forwarding." -ForegroundColor Yellow
Write-Host "Keep them open while using the application." -ForegroundColor Yellow
Write-Host ""
Write-Host "To stop port forwarding, close those windows or press Ctrl+C in them." -ForegroundColor White
Write-Host ""
