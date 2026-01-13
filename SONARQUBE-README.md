# SonarQube (local) — setup and analysis

This repo includes a local SonarQube setup (Docker Compose) and scripts to run analysis.

## What was done

- Added `docker-compose-sonar.yml` (SonarQube + PostgreSQL)
- Added scripts:
  - `start-sonarqube.ps1`
  - `run-sonarqube-analysis.ps1`
- Added configs:
  - `sonar-project.properties`
  - `GymHiveFrontend/sonar-project.properties`

## Start SonarQube

```powershell
./start-sonarqube.ps1
```

Check readiness:

```powershell
docker logs gymhive-sonarqube
```

Open:

- `http://localhost:9000`

Default login is typically `admin` / `admin` (then change password).

## Create a token

In the UI:

- Profile → My Account → Security → Generate Token

Set env var:

```powershell
$env:SONAR_TOKEN = "<your-token>"
```

## Run analysis

```powershell
./run-sonarqube-analysis.ps1
```

## Stop

```powershell
docker-compose -f docker-compose-sonar.yml down
```

## Troubleshooting

- If the UI is not reachable, wait a few minutes and re-check logs.
- If it runs out of memory, increase Docker Desktop memory (SonarQube can be heavy).
