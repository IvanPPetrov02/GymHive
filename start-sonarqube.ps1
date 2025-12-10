# Quick Start SonarQube for GymHive
Write-Host "Starting SonarQube for GymHive..." -ForegroundColor Cyan
Write-Host ""

# Start SonarQube
Write-Host "1. Starting SonarQube container..." -ForegroundColor Yellow
docker-compose -f docker-compose-sonar.yml up -d

if ($LASTEXITCODE -ne 0) {
    Write-Host "Failed to start SonarQube. Make sure Docker Desktop is running." -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "✓ SonarQube is starting..." -ForegroundColor Green
Write-Host ""
Write-Host "Please wait 2-3 minutes for SonarQube to fully start." -ForegroundColor Yellow
Write-Host ""
Write-Host "Then:" -ForegroundColor Cyan
Write-Host "  1. Open: http://localhost:9000" -ForegroundColor White
Write-Host "  2. Login with: admin / admin" -ForegroundColor White
Write-Host "  3. Change password when prompted" -ForegroundColor White
Write-Host "  4. Generate a token: My Account → Security → Generate Tokens" -ForegroundColor White
Write-Host "  5. Run analysis: .\run-sonarqube-analysis.ps1" -ForegroundColor White
Write-Host ""
Write-Host "To check if ready, run: docker logs gymhive-sonarqube" -ForegroundColor Yellow
Write-Host ""
