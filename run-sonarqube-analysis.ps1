# GymHive Microservices SonarQube Analysis
# Analyzes each microservice separately

Write-Host "============================================" -ForegroundColor Cyan
Write-Host " GymHive Microservices SonarQube Analysis" -ForegroundColor Cyan
Write-Host "============================================" -ForegroundColor Cyan
Write-Host ""

# Check SonarQube
Write-Host "Checking SonarQube..." -ForegroundColor Yellow
try {
    $response = Invoke-WebRequest -Uri "http://localhost:9000/api/system/status" -Method GET -ErrorAction Stop -UseBasicParsing
    Write-Host "Success SonarQube is running" -ForegroundColor Green
}
catch {
    Write-Host "Error SonarQube not running" -ForegroundColor Red
    Write-Host "Start: docker-compose -f docker-compose-sonar.yml up -d" -ForegroundColor Yellow
    exit 1
}

Write-Host ""

# Check scanner
if (!(Get-Command "dotnet-sonarscanner" -ErrorAction SilentlyContinue)) {
    Write-Host "Installing dotnet-sonarscanner..." -ForegroundColor Yellow
    dotnet tool install --global dotnet-sonarscanner
}

# Get token
$sonarToken = ""
if (Test-Path ".env") {
    $envContent = Get-Content ".env"
    foreach ($line in $envContent) {
        if ($line -match '^SONAR_TOKEN=(.+)$') {
            $sonarToken = $matches[1].Trim()
            break
        }
    }
}

if ([string]::IsNullOrEmpty($sonarToken)) {
    Write-Host "Using admin/admin auth" -ForegroundColor Yellow
    $useToken = $false
}
else {
    Write-Host "Using token auth" -ForegroundColor Green
    $useToken = $true
}

Write-Host ""

# Services
$services = @(
    @{Name="AuthenticationService"; Key="gymhive-auth"},
    @{Name="GymService"; Key="gymhive-gym"},
    @{Name="MembershipService"; Key="gymhive-membership"},
    @{Name="NotificationsService"; Key="gymhive-notifications"},
    @{Name="WorkoutLoggingService"; Key="gymhive-workout"},
    @{Name="ApiGateway"; Key="gymhive-gateway"},
    @{Name="GymHive.Messaging"; Key="gymhive-messaging"}
)

$success = 0
$failed = @()

foreach ($svc in $services) {
    Write-Host "----------------------------------------" -ForegroundColor Cyan
    Write-Host "Analyzing: $($svc.Name)" -ForegroundColor Cyan
    Write-Host "----------------------------------------" -ForegroundColor Cyan
    
    $path = "GymHiveBackend\$($svc.Name)"
    if (!(Test-Path $path)) {
        Write-Host "Path not found: $path" -ForegroundColor Red
        $failed += $svc.Name
        continue
    }
    
    $csproj = Get-ChildItem -Path $path -Filter "$($svc.Name).csproj" -File | Select-Object -First 1
    if (!$csproj) {
        $csproj = Get-ChildItem -Path $path -Filter "*.csproj" -File | Select-Object -First 1
    }
    
    if (!$csproj) {
        Write-Host "No csproj found" -ForegroundColor Red
        $failed += $svc.Name
        continue
    }
    
    Set-Location $path
    
    # Begin
    Write-Host "Beginning analysis..." -ForegroundColor Yellow
    if ($useToken) {
        dotnet-sonarscanner begin /k:"$($svc.Key)" /n:"$($svc.Name)" /d:sonar.host.url="http://localhost:9000" /d:sonar.token="$sonarToken" | Out-Null
    }
    else {
        dotnet-sonarscanner begin /k:"$($svc.Key)" /n:"$($svc.Name)" /d:sonar.host.url="http://localhost:9000" /d:sonar.login="admin" /d:sonar.password="admin" | Out-Null
    }
    
    if ($LASTEXITCODE -ne 0) {
        Write-Host "Begin failed" -ForegroundColor Red
        $failed += $svc.Name
        Set-Location $PSScriptRoot
        continue
    }
    
    # Build
    Write-Host "Building..." -ForegroundColor Yellow
    dotnet build $csproj.Name --no-incremental --verbosity quiet
    
    if ($LASTEXITCODE -ne 0) {
        Write-Host "Build failed" -ForegroundColor Red
        $failed += $svc.Name
        Set-Location $PSScriptRoot
        continue
    }
    
    # End
    Write-Host "Uploading..." -ForegroundColor Yellow
    if ($useToken) {
        dotnet-sonarscanner end /d:sonar.token="$sonarToken" | Out-Null
    }
    else {
        dotnet-sonarscanner end /d:sonar.login="admin" /d:sonar.password="admin" | Out-Null
    }
    
    if ($LASTEXITCODE -ne 0) {
        Write-Host "Upload failed" -ForegroundColor Red
        $failed += $svc.Name
    }
    else {
        Write-Host "SUCCESS" -ForegroundColor Green
        $success++
    }
    
    Set-Location $PSScriptRoot
    Write-Host ""
}

# Summary
Write-Host "============================================" -ForegroundColor Cyan
Write-Host " Summary" -ForegroundColor Cyan
Write-Host "============================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Successful: $success / $($services.Count)" -ForegroundColor Green

if ($failed.Count -gt 0) {
    Write-Host "Failed:" -ForegroundColor Red
    foreach ($f in $failed) {
        Write-Host "  - $f" -ForegroundColor Red
    }
}

Write-Host ""
Write-Host "View at: http://localhost:9000" -ForegroundColor Yellow
Write-Host ""
Write-Host "Dashboards:" -ForegroundColor Yellow
foreach ($svc in $services) {
    if ($failed -notcontains $svc.Name) {
        Write-Host "  $($svc.Name): http://localhost:9000/dashboard?id=$($svc.Key)" -ForegroundColor Cyan
    }
}
Write-Host ""
