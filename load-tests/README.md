# GymHive — load testing (k6)

This folder contains k6 load test scripts and PowerShell runners for GymHive.

## What was done

- Added k6 scripts under `load-tests/tests/`.
- Added runner scripts under `load-tests/runners/` for Docker Compose and Kubernetes.
- Added a results folder under `load-tests/results/`.

## Prerequisites

Install k6:

- Windows (Chocolatey): `choco install k6`
- Windows (winget): `winget install k6 --source winget`
- macOS (Homebrew): `brew install k6`

You also need a running GymHive deployment (Docker Compose or Kubernetes).

## Structure

```
load-tests/
  tests/        # k6 scripts
  runners/      # PowerShell automation
  results/      # output artifacts
  QUICK_START.md
```

## Run against Docker Compose

Start the stack (from repo root):

```powershell
cd deployment/docker
docker-compose up -d
```

Run tests:

```powershell
cd load-tests/runners
./docker.ps1 -TestType auth
./docker.ps1 -TestType full-system
./docker.ps1 -TestType all
```

## Run against Kubernetes

Deploy first (example):

```powershell
cd deployment/kubernetes
./deploy-k8s.ps1
```

Run tests:

```powershell
cd load-tests/runners
./kubernetes.ps1 -TestType auth
./kubernetes.ps1 -TestType full-system
./kubernetes.ps1 -TestType all
```

## Notes

- If you change ingress/ports, update the runners or test config accordingly.
- For reporting, prefer committing summaries instead of large raw outputs.
