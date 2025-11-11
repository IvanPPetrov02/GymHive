# Run GymHive Load Tests - Docker Compose
# Usage: .\run-docker-tests-v2.ps1 -TestType [auth|full-system|all] [-Quiet]

param(
    [Parameter(Mandatory=$true)]
    [ValidateSet("auth", "full-system", "all")]
    [string]$TestType,
    
    [Parameter(Mandatory=$false)]
    [switch]$Quiet
)

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  GymHive Load Tests - Docker Compose  " -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan

# Check if Docker is running
Write-Host "`nChecking Docker status..." -ForegroundColor Yellow
try {
    docker ps | Out-Null
    Write-Host "Docker is running" -ForegroundColor Green
} catch {
    Write-Host "Docker is not running. Please start Docker Desktop." -ForegroundColor Red
    exit 1
}

# Check if docker-compose services are running
Write-Host "`nChecking GymHive services..." -ForegroundColor Yellow
$services = @("api-gateway", "authentication-service", "gym-service")
$allRunning = $true

foreach ($service in $services) {
    $container = docker ps --filter "name=$service" --format "{{.Names}}" 2>$null
    if ($container) {
        Write-Host "  Service running: $service" -ForegroundColor Green
    } else {
        Write-Host "  Service NOT running: $service" -ForegroundColor Red
        $allRunning = $false
    }
}

if (-not $allRunning) {
    Write-Host "`nStarting services with docker-compose..." -ForegroundColor Yellow
    docker-compose -f ../../deployment/docker/docker-compose.yml up -d
    Write-Host "Waiting 30 seconds for services to initialize..." -ForegroundColor Yellow
    Start-Sleep -Seconds 30
}

# Test API Gateway
Write-Host "`nTesting API Gateway connectivity..." -ForegroundColor Yellow
try {
    $response = Invoke-WebRequest -Uri "http://localhost:5000/health" -UseBasicParsing -TimeoutSec 5
    Write-Host "API Gateway is healthy" -ForegroundColor Green
} catch {
    Write-Host "API Gateway is not responding" -ForegroundColor Red
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
    Write-Host "Environment: Docker Compose" -ForegroundColor Gray
    Write-Host "Target: 150 peak VUs" -ForegroundColor Gray
    Write-Host "Duration: ~5 minutes`n" -ForegroundColor Gray
    
    $env:ENVIRONMENT = "docker"
    $env:BASE_URL = "http://localhost:5000"
    
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
    Write-Host "Environment: Docker Compose" -ForegroundColor Gray
    Write-Host "Target: 150 peak VUs" -ForegroundColor Gray
    Write-Host "Duration: ~6 minutes`n" -ForegroundColor Gray
    
    $env:ENVIRONMENT = "docker"
    $env:BASE_URL = "http://localhost:5000"
    
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

$resultFiles = Get-ChildItem -Path "../results" -Filter "docker-*.md" | Sort-Object LastWriteTime -Descending | Select-Object -First 5

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
