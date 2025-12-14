# GymHive Services Startup Script
# This script starts all microservices in separate PowerShell windows

Write-Host "Starting GymHive Microservices..." -ForegroundColor Green
Write-Host ""

# Define base path
$basePath = $PSScriptRoot
$backendPath = Join-Path $basePath "GymHiveBackend"

# Function to start a service in a new window
function Start-Service {
    param(
        [string]$ServiceName,
        [string]$ServicePath,
        [int]$Port
    )
    
    $fullPath = Join-Path $backendPath $ServicePath
    
    Write-Host "Starting $ServiceName on port $Port..." -ForegroundColor Cyan
    
    Start-Process powershell -ArgumentList @(
        "-NoExit",
        "-Command",
        "cd '$fullPath'; Write-Host '$ServiceName Running on Port $Port' -ForegroundColor Green; dotnet run"
    ) -WindowStyle Normal
    
    Start-Sleep -Milliseconds 500
}

Write-Host "Service Startup Order:" -ForegroundColor Yellow
Write-Host "  1. AuthenticationService (Port 5010)" -ForegroundColor Gray
Write-Host "  2. GymService (Port 5001)" -ForegroundColor Gray
Write-Host "  3. MembershipService (Port 5002)" -ForegroundColor Gray
Write-Host "  4. NotificationsService (Port 5003)" -ForegroundColor Gray
Write-Host "  5. WorkoutLoggingService (Port 5004)" -ForegroundColor Gray
Write-Host "  6. API Gateway (Port 5000)" -ForegroundColor Gray
Write-Host ""

# Start services in order
Start-Service "AuthenticationService" "AuthenticationService" 5010
Start-Sleep -Seconds 5

Start-Service "GymService" "GymService" 5001
Start-Sleep -Seconds 3

Start-Service "MembershipService" "MembershipService" 5002
Start-Sleep -Seconds 3

Start-Service "NotificationsService" "NotificationsService" 5003
Start-Sleep -Seconds 3

Start-Service "WorkoutLoggingService" "WorkoutLoggingService" 5004
Start-Sleep -Seconds 3

Start-Service "API Gateway" "ApiGateway" 5000

Write-Host ""
Write-Host "All services are starting up!" -ForegroundColor Green
Write-Host ""
Write-Host "Access Points:" -ForegroundColor Yellow
Write-Host "  Gateway:        http://localhost:5000" -ForegroundColor White
Write-Host "  Auth:           http://localhost:5010/swagger" -ForegroundColor White
Write-Host "  Gym:            http://localhost:5001/swagger" -ForegroundColor White
Write-Host "  Membership:     http://localhost:5002/swagger" -ForegroundColor White
Write-Host "  Notifications:  http://localhost:5003/swagger" -ForegroundColor White
Write-Host "  Workouts:       http://localhost:5004/swagger" -ForegroundColor White
Write-Host ""
Write-Host "Postman Collection: GymHive-Postman-Collection.json" -ForegroundColor Cyan
Write-Host ""
Write-Host "Wait 30-60 seconds for all services to fully start..." -ForegroundColor Magenta
Write-Host ""
Write-Host "Press any key to close this window (services will continue running)..." -ForegroundColor Gray
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
