# ============================================================================
# Script: Run Tests with Coverage for SonarQube
# Description: Runs all unit tests with code coverage and uploads to SonarQube
# ============================================================================

Write-Host "===============================================" -ForegroundColor Cyan
Write-Host " Running Tests with Coverage for All Services" -ForegroundColor Cyan
Write-Host "===============================================" -ForegroundColor Cyan
Write-Host ""

$scriptRoot = Get-Location

# Read .env file
$envFile = Join-Path $scriptRoot ".env"
if (Test-Path $envFile) {
    Get-Content $envFile | ForEach-Object {
        if ($_ -match '^SONAR_TOKEN=(.*)$') {
            $sonarToken = $matches[1]
        }
        if ($_ -match '^SONAR_HOST_URL=(.*)$') {
            $sonarUrl = $matches[1]
        }
    }
}

if (-not $sonarToken) {
    Write-Host "ERROR: SONAR_TOKEN not found in .env file" -ForegroundColor Red
    exit 1
}

if (-not $sonarUrl) {
    $sonarUrl = "http://localhost:9000"
}

Write-Host "SonarQube URL: $sonarUrl" -ForegroundColor Yellow

# Check SonarQube
try {
    $response = Invoke-WebRequest -Uri "$sonarUrl/api/system/status" -TimeoutSec 5 -UseBasicParsing
    Write-Host "SonarQube is running" -ForegroundColor Green
} catch {
    Write-Host "SonarQube not accessible" -ForegroundColor Red
    exit 1
}

# Define services
$services = @(
    @{Name="AuthenticationService"; Key="gymhive-auth"; HasTests=$true},
    @{Name="GymService"; Key="gymhive-gym"; HasTests=$true},
    @{Name="MembershipService"; Key="gymhive-membership"; HasTests=$true},
    @{Name="NotificationsService"; Key="gymhive-notifications"; HasTests=$true},
    @{Name="WorkoutLoggingService"; Key="gymhive-workout"; HasTests=$true}
)

$successCount = 0
$failureCount = 0

foreach ($svc in $services) {
    Write-Host ""
    Write-Host "============================================" -ForegroundColor Cyan
    Write-Host " Processing: $($svc.Name)" -ForegroundColor Cyan
    Write-Host "============================================" -ForegroundColor Cyan
    
    $servicePath = Join-Path $scriptRoot "GymHiveBackend\$($svc.Name)"
    
    if (-not (Test-Path $servicePath)) {
        Write-Host "Path not found: $servicePath" -ForegroundColor Yellow
        continue
    }
    
    Set-Location $servicePath
    
    $csprojFile = "$($svc.Name).csproj"
    if (-not (Test-Path $csprojFile)) {
        Write-Host "Project file not found" -ForegroundColor Yellow
        Set-Location $scriptRoot
        continue
    }
    
    $testResultsPath = Join-Path $servicePath "TestResults"
    if (Test-Path $testResultsPath) {
        Remove-Item $testResultsPath -Recurse -Force
    }
    
    try {
        # Begin SonarQube
        Write-Host "Beginning SonarQube analysis..." -ForegroundColor Yellow
        dotnet-sonarscanner begin `
            /k:"$($svc.Key)" `
            /d:sonar.host.url="$sonarUrl" `
            /d:sonar.token="$sonarToken" `
            /d:sonar.cs.opencover.reportsPaths="TestResults\**\coverage.opencover.xml"
        
        if ($LASTEXITCODE -ne 0) {
            throw "SonarScanner begin failed"
        }
        
        # Build
        Write-Host "Building..." -ForegroundColor Yellow
        dotnet build $csprojFile --no-incremental
        
        if ($LASTEXITCODE -ne 0) {
            throw "Build failed"
        }
        
        # Test with coverage
        if ($svc.HasTests) {
            Write-Host "Running tests with coverage..." -ForegroundColor Yellow
            dotnet test $csprojFile `
                --no-build `
                --collect:"XPlat Code Coverage" `
                --results-directory $testResultsPath `
                -- DataCollectionRunSettings.DataCollectors.DataCollector.Configuration.Format=opencover
            
            $coverageFiles = Get-ChildItem -Path $testResultsPath -Filter "coverage.opencover.xml" -Recurse -ErrorAction SilentlyContinue
            if ($coverageFiles.Count -gt 0) {
                Write-Host "Generated $($coverageFiles.Count) coverage file(s)" -ForegroundColor Green
            }
        }
        
        # End SonarQube
        Write-Host "Uploading to SonarQube..." -ForegroundColor Yellow
        dotnet-sonarscanner end /d:sonar.token="$sonarToken"
        
        if ($LASTEXITCODE -ne 0) {
            throw "SonarScanner end failed"
        }
        
        Write-Host "SUCCESS: $($svc.Name)" -ForegroundColor Green
        $successCount++
        
    } catch {
        Write-Host "FAILED: $($svc.Name) - $_" -ForegroundColor Red
        $failureCount++
    }
    
    Set-Location $scriptRoot
}

# Summary
Write-Host ""
Write-Host "============================================" -ForegroundColor Cyan
Write-Host " Summary" -ForegroundColor Cyan
Write-Host "============================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Successful: $successCount / $($services.Count)" -ForegroundColor Green
Write-Host "Failed: $failureCount" -ForegroundColor $(if ($failureCount -eq 0) { "Green" } else { "Red" })
Write-Host ""
Write-Host "View results at: $sonarUrl" -ForegroundColor Cyan
