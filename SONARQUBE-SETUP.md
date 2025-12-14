# GymHive SonarQube Setup Guide

## Prerequisites
- Docker Desktop installed and running
- .NET SDK 9.0+ installed
- Node.js 18+ and npm installed
- PowerShell (Windows) or equivalent shell

## Quick Start

### 1. Start SonarQube Server
```powershell
docker-compose -f docker-compose-sonar.yml up -d
```

Wait 2-3 minutes for SonarQube to start.

### 2. Access SonarQube
- Open browser: http://localhost:9000
- Default credentials: **admin** / **admin**
- You'll be prompted to change password on first login

### 3. Generate Authentication Token (Recommended)
1. Login to SonarQube (http://localhost:9000)
2. Go to: My Account → Security → Generate Tokens
3. Name: "GymHive Analysis"
4. Type: User Token
5. Click "Generate"
6. Copy the token
7. Set environment variable:
   ```powershell
   $env:SONAR_TOKEN = "your-token-here"
   ```

### 4. Run Analysis
```powershell
.\run-sonarqube-analysis.ps1
```

## What Gets Analyzed

### Backend (.NET Microservices)
- ✅ C# code quality and security
- ✅ Code smells and bugs
- ✅ Security vulnerabilities
- ✅ Code coverage (if tests run)
- ✅ Duplicate code detection
- ✅ Maintainability metrics

**Services Analyzed:**
- AuthenticationService
- GymService
- MembershipService
- NotificationsService
- WorkoutLoggingService
- API Gateway
- Shared libraries (BLL, DAL, Messaging)

### Frontend (Svelte + TypeScript)
- ✅ TypeScript/JavaScript code quality
- ✅ Code smells and bugs
- ✅ Security vulnerabilities
- ✅ Duplicate code detection
- ✅ Maintainability metrics

## Manual Analysis Steps

### Backend Only
```powershell
cd GymHiveBackend

# Begin analysis
dotnet-sonarscanner begin `
  /k:"gymhive-backend" `
  /d:sonar.host.url="http://localhost:9000" `
  /d:sonar.token="YOUR_TOKEN"

# Build
dotnet build

# Run tests (optional)
dotnet test --collect:"XPlat Code Coverage"

# End analysis
dotnet-sonarscanner end /d:sonar.token="YOUR_TOKEN"
```

### Frontend Only
```powershell
cd GymHiveFrontend

# Run analysis
sonar-scanner `
  -Dsonar.projectKey=gymhive-frontend `
  -Dsonar.sources=src `
  -Dsonar.host.url=http://localhost:9000 `
  -Dsonar.token=YOUR_TOKEN
```

## View Results

After analysis completes:

1. **Overall Dashboard**: http://localhost:9000
2. **Backend**: http://localhost:9000/dashboard?id=gymhive-backend
3. **Frontend**: http://localhost:9000/dashboard?id=gymhive-frontend

## Metrics Tracked

- **Bugs**: Logic errors that will cause incorrect behavior
- **Vulnerabilities**: Security weaknesses
- **Code Smells**: Maintainability issues
- **Coverage**: Test coverage percentage
- **Duplications**: Duplicated code blocks
- **Security Hotspots**: Security-sensitive code to review

## Quality Gate

Default quality gate checks:
- ✅ No new bugs
- ✅ No new vulnerabilities
- ✅ Security rating A
- ✅ Maintainability rating A
- ✅ Code coverage > 80% (configurable)
- ✅ Duplicated lines < 3%

## Troubleshooting

### SonarQube won't start
```powershell
# Check Docker logs
docker logs gymhive-sonarqube

# Restart containers
docker-compose -f docker-compose-sonar.yml down
docker-compose -f docker-compose-sonar.yml up -d
```

### Analysis fails
1. Ensure SonarQube is running (check http://localhost:9000)
2. Verify token is set correctly
3. Check firewall isn't blocking port 9000
4. Review error messages in console

### Out of memory
Edit `docker-compose-sonar.yml` and add:
```yaml
services:
  sonarqube:
    environment:
      - SONAR_ES_BOOTSTRAP_CHECKS_DISABLE=true
```

## Stop SonarQube

```powershell
docker-compose -f docker-compose-sonar.yml down
```

To remove all data:
```powershell
docker-compose -f docker-compose-sonar.yml down -v
```

## Integration with CI/CD

You can integrate this into your CI/CD pipeline:
1. Use the same scripts in GitHub Actions/Azure DevOps
2. Set SONAR_TOKEN as secret environment variable
3. Run analysis on every pull request
4. Block merges if quality gate fails

## Additional Resources

- SonarQube Documentation: https://docs.sonarqube.org/
- .NET Scanner: https://docs.sonarqube.org/latest/analysis/scan/sonarscanner-for-msbuild/
- JavaScript/TypeScript: https://docs.sonarqube.org/latest/analysis/languages/javascript/
