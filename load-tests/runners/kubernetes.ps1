# Run Load Tests for Kubernetes Environment
# Usage: .\run-kubernetes-tests.ps1

param(
    [string]$TestType = "all",  # Options: auth, full-system, all
    [switch]$Quiet,
    [string]$Namespace = "gymhive"
)

$ErrorActionPreference = "Stop"

Write-Host "`n╔════════════════════════════════════════════════════════════╗" -ForegroundColor Cyan
Write-Host "║           KUBERNETES LOAD TEST RUNNER                      ║" -ForegroundColor Cyan
Write-Host "╚════════════════════════════════════════════════════════════╝`n" -ForegroundColor Cyan

# Check if kubectl is available
Write-Host "🔍 Checking Kubernetes cluster..." -ForegroundColor Yellow
try {
    $nodes = kubectl get nodes -o json | ConvertFrom-Json
    Write-Host "✅ Kubernetes cluster is accessible" -ForegroundColor Green
} catch {
    Write-Host "❌ Cannot connect to Kubernetes cluster" -ForegroundColor Red
    Write-Host "   Make sure kubectl is configured and cluster is running" -ForegroundColor Gray
    exit 1
}

# Check if namespace exists
Write-Host "🔍 Checking namespace '$Namespace'..." -ForegroundColor Yellow
try {
    $ns = kubectl get namespace $Namespace -o json 2>$null | ConvertFrom-Json
    Write-Host "✅ Namespace '$Namespace' exists" -ForegroundColor Green
} catch {
    Write-Host "❌ Namespace '$Namespace' not found" -ForegroundColor Red
    Write-Host "   Deploy Kubernetes first: cd ..\deployment\kubernetes; .\deploy-k8s.ps1" -ForegroundColor Gray
    exit 1
}

# Check if pods are running
Write-Host "🔍 Checking pod status..." -ForegroundColor Yellow
$pods = kubectl get pods -n $Namespace -o json | ConvertFrom-Json
$runningPods = $pods.items | Where-Object { $_.status.phase -eq "Running" }

if ($runningPods.Count -lt 5) {
    Write-Host "⚠️  Not all pods are running. Current status:" -ForegroundColor Yellow
    kubectl get pods -n $Namespace
    Write-Host "`nDo you want to continue anyway? (y/n): " -ForegroundColor Yellow -NoNewline
    $continue = Read-Host
    if ($continue -ne "y") {
        exit 1
    }
} else {
    Write-Host "✅ All pods are running ($($runningPods.Count) pods)" -ForegroundColor Green
}

# Determine the base URL (NodePort)
$apiGatewaySvc = kubectl get service api-gateway -n $Namespace -o json | ConvertFrom-Json
$nodePort = $apiGatewaySvc.spec.ports[0].nodePort
$baseUrl = "http://localhost:$nodePort"

Write-Host "`n🔍 Testing API Gateway connectivity..." -ForegroundColor Yellow
Write-Host "   URL: $baseUrl/health" -ForegroundColor Gray
try {
    $response = Invoke-WebRequest -Uri "$baseUrl/health" -UseBasicParsing -TimeoutSec 5
    Write-Host "✅ API Gateway is healthy (Status: $($response.StatusCode))" -ForegroundColor Green
} catch {
    Write-Host "❌ API Gateway is not responding" -ForegroundColor Red
    Write-Host "   Try: kubectl logs deployment/api-gateway -n $Namespace" -ForegroundColor Gray
    exit 1
}

# Create results directory if it doesn't exist
if (-not (Test-Path "../results")) {
    New-Item -ItemType Directory -Path "../results" | Out-Null
}

# Run tests based on TestType
if ($TestType -eq "auth" -or $TestType -eq "all") {
    Write-Host "`n📊 Running Authentication Load Test..." -ForegroundColor Cyan
    Write-Host "   Environment: Kubernetes" -ForegroundColor Gray
    Write-Host "   Target: 500 peak VUs" -ForegroundColor Gray
    Write-Host "   Duration: ~9 minutes`n" -ForegroundColor Gray
    
    $env:ENVIRONMENT = "kubernetes"
    $env:BASE_URL = $baseUrl
    
    if ($Quiet) {
        k6 run ../tests/auth-test.js --quiet
    } else {
        k6 run ../tests/auth-test.js
    }
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "`n✅ Authentication test completed successfully" -ForegroundColor Green
    } else {
        Write-Host "`n⚠️  Test completed with warnings (exit code: $LASTEXITCODE)" -ForegroundColor Yellow
    }
}

if ($TestType -eq "full-system" -or $TestType -eq "all") {
    Write-Host "`n📊 Running Full System Load Test..." -ForegroundColor Cyan
    Write-Host "   Environment: Kubernetes" -ForegroundColor Gray
    Write-Host "   Target: 700 peak VUs" -ForegroundColor Gray
    Write-Host "   Duration: ~11 minutes`n" -ForegroundColor Gray
    
    $env:ENVIRONMENT = "kubernetes"
    $env:BASE_URL = $baseUrl
    
    if ($Quiet) {
        k6 run ../tests/full-system-test.js --quiet
    } else {
        k6 run ../tests/full-system-test.js
    }
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "`n✅ Full system test completed successfully" -ForegroundColor Green
    } else {
        Write-Host "`n⚠️  Test completed with warnings (exit code: $LASTEXITCODE)" -ForegroundColor Yellow
    }
}

Write-Host "`n╔════════════════════════════════════════════════════════════╗" -ForegroundColor Green
Write-Host "║              ALL TESTS COMPLETED                           ║" -ForegroundColor Green
Write-Host "╚════════════════════════════════════════════════════════════╝`n" -ForegroundColor Green

Write-Host "📁 Results saved in: ..\results\" -ForegroundColor Cyan
Write-Host "   View .md files for formatted reports" -ForegroundColor Gray
Write-Host "   View .json files for raw data`n" -ForegroundColor Gray

# List generated files
$recentFiles = Get-ChildItem -Path "../results" -Filter "kubernetes-*" | 
    Sort-Object LastWriteTime -Descending | 
    Select-Object -First 6

if ($recentFiles) {
    Write-Host "📊 Recent test results:" -ForegroundColor Cyan
    foreach ($file in $recentFiles) {
        $size = "{0:N2} KB" -f ($file.Length / 1KB)
        Write-Host "   • $($file.Name) ($size)" -ForegroundColor White
    }
}

Write-Host "`n💡 Tip: Monitor pods during test with:" -ForegroundColor Cyan
Write-Host "   kubectl get pods -n $Namespace -w" -ForegroundColor Gray
Write-Host "   kubectl top pods -n $Namespace`n" -ForegroundColor Gray
