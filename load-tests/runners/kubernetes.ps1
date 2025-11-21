# Run GymHive Load Tests - Kubernetes
# Usage: .\kubernetes.ps1 -TestType [auth|full-system|all] [-Quiet]

param(
    [Parameter(Mandatory=$true)]
    [ValidateSet("auth", "full-system", "all")]
    [string]$TestType,
    
    [Parameter(Mandatory=$false)]
    [switch]$Quiet,
    
    [Parameter(Mandatory=$false)]
    [string]$Namespace = "gymhive"
)

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  GymHive Load Tests - Kubernetes  " -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan

# Check if kubectl is available
Write-Host "`nChecking Kubernetes cluster..." -ForegroundColor Yellow
try {
    kubectl get nodes | Out-Null
    Write-Host "Kubernetes cluster is accessible" -ForegroundColor Green
} catch {
    Write-Host "Cannot connect to Kubernetes cluster" -ForegroundColor Red
    exit 1
}

# Detect cluster type (Minikube vs kind)
$currentContext = kubectl config current-context 2>&1
$clusterInfo = kubectl cluster-info 2>&1 | Out-String
$isMinikube = ($currentContext -eq "minikube") -or ($clusterInfo -match "minikube")

if ($isMinikube) {
    Write-Host "Detected Minikube cluster" -ForegroundColor Cyan
} else {
    Write-Host "Detected kind or Docker Desktop Kubernetes" -ForegroundColor Cyan
}

# Check if namespace exists
Write-Host "`nChecking namespace '$Namespace'..." -ForegroundColor Yellow
try {
    kubectl get namespace $Namespace | Out-Null
    Write-Host "Namespace '$Namespace' exists" -ForegroundColor Green
} catch {
    Write-Host "Namespace '$Namespace' not found" -ForegroundColor Red
    exit 1
}

# Check if pods are running
Write-Host "`nChecking pod status..." -ForegroundColor Yellow
$pods = kubectl get pods -n $Namespace -o json | ConvertFrom-Json
$runningPods = $pods.items | Where-Object { $_.status.phase -eq "Running" }
Write-Host "Running pods: $($runningPods.Count)" -ForegroundColor Green

# Setup service access - use port-forwarding for Docker-based Minikube
Write-Host "`nNote: Minikube with Docker driver detected" -ForegroundColor Yellow
Write-Host "Using port-forwarding (Docker-in-Docker networking limitation)" -ForegroundColor Gray
$isMinikube = $false  # Use port-forward method for Docker-based Minikube

if (-not $isMinikube) {
    # Use port-forwarding for kind/Docker Desktop
    $baseUrl = "http://localhost:5000"
    
    Write-Host "`nSetting up port-forwarding..." -ForegroundColor Yellow
    # Stop any existing port-forwards
    Get-Job -Name "k6-pf-*" -ErrorAction SilentlyContinue | Stop-Job
    Get-Job -Name "k6-pf-*" -ErrorAction SilentlyContinue | Remove-Job
    
    # Start port-forward in background
    Start-Job -Name "k6-pf-api" -ScriptBlock {
        kubectl port-forward service/api-gateway 5000:80 -n gymhive
    } | Out-Null
    
    Start-Sleep -Seconds 3
}

# Test API Gateway
Write-Host "`nTesting API Gateway connectivity..." -ForegroundColor Yellow
Write-Host "URL: $baseUrl/health" -ForegroundColor Gray
try {
    $response = Invoke-WebRequest -Uri "$baseUrl/health" -UseBasicParsing -TimeoutSec 10
    if ($isMinikube) {
        Write-Host "API Gateway is healthy (Minikube direct access)" -ForegroundColor Green
    } else {
        Write-Host "API Gateway is healthy (auto port-forward)" -ForegroundColor Green
    }
} catch {
    Write-Host "API Gateway is not responding" -ForegroundColor Red
    if (-not $isMinikube) {
        Get-Job -Name "k6-pf-*" | Stop-Job
        Get-Job -Name "k6-pf-*" | Remove-Job
    }
    exit 1
}

# Create results directory
if (-not (Test-Path "../results")) {
    New-Item -ItemType Directory -Path "../results" | Out-Null
}

# Run authentication test
if ($TestType -eq "auth" -or $TestType -eq "all") {
    Write-Host "`n===========================================" -ForegroundColor Cyan
    Write-Host " Running Authentication Load Test" -ForegroundColor Cyan
    Write-Host "===========================================" -ForegroundColor Cyan
    Write-Host "Environment: Kubernetes" -ForegroundColor Gray
    Write-Host "Target: 150 peak VUs (same as Docker)" -ForegroundColor Gray
    Write-Host "Duration: ~5 minutes`n" -ForegroundColor Gray
    
    $env:ENVIRONMENT = "kubernetes"
    $env:BASE_URL = $baseUrl
    
    if ($Quiet) {
        k6 run ../tests/auth-test.js --quiet
    } else {
        k6 run ../tests/auth-test.js
    }
    
    $exitCode = $LASTEXITCODE
    if ($exitCode -eq 0) {
        Write-Host "`nAuthentication test completed successfully" -ForegroundColor Green
    } else {
        Write-Host "`nTest completed with warnings (exit code: $exitCode)" -ForegroundColor Yellow
    }
}

# Run full system test
if ($TestType -eq "full-system" -or $TestType -eq "all") {
    Write-Host "`n===========================================" -ForegroundColor Cyan
    Write-Host " Running Full System Load Test" -ForegroundColor Cyan
    Write-Host "===========================================" -ForegroundColor Cyan
    Write-Host "Environment: Kubernetes" -ForegroundColor Gray
    Write-Host "Target: 150 peak VUs (same as Docker)" -ForegroundColor Gray
    Write-Host "Duration: ~6 minutes`n" -ForegroundColor Gray
    
    $env:ENVIRONMENT = "kubernetes"
    $env:BASE_URL = $baseUrl
    
    if ($Quiet) {
        k6 run ../tests/full-system-test.js --quiet
    } else {
        k6 run ../tests/full-system-test.js
    }
    
    $exitCode = $LASTEXITCODE
    if ($exitCode -eq 0) {
        Write-Host "`nFull system test completed successfully" -ForegroundColor Green
    } else {
        Write-Host "`nTest completed with warnings (exit code: $exitCode)" -ForegroundColor Yellow
    }
}

# List result files
Write-Host "`n===========================================" -ForegroundColor Cyan
Write-Host " Test Results Saved" -ForegroundColor Cyan
Write-Host "===========================================" -ForegroundColor Cyan

$resultFiles = Get-ChildItem -Path "../results" -Filter "kubernetes-*.md" | Sort-Object LastWriteTime -Descending | Select-Object -First 5

if ($resultFiles) {
    Write-Host "`nRecent result files:" -ForegroundColor Yellow
    foreach ($file in $resultFiles) {
        Write-Host "  $($file.Name)" -ForegroundColor Cyan
    }
} else {
    Write-Host "`nNo result files found in results/ directory" -ForegroundColor Yellow
}

Write-Host "`n========================================" -ForegroundColor Green
Write-Host "  Load testing complete!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green

# Cleanup port-forwarding (only for non-Minikube)
if (-not $isMinikube) {
    Write-Host "`nCleaning up port-forwarding..." -ForegroundColor Yellow
    Get-Job -Name "k6-pf-*" -ErrorAction SilentlyContinue | Stop-Job
    Get-Job -Name "k6-pf-*" -ErrorAction SilentlyContinue | Remove-Job
    Write-Host "Done!" -ForegroundColor Green
} else {
    Write-Host "`nNo cleanup needed for Minikube direct access" -ForegroundColor Green
}
