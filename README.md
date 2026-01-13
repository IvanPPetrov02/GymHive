# GymHive

GymHive is a microservices-based gym management platform with a Svelte frontend and an ASP.NET Core backend behind an API Gateway.

## What was built

- Frontend: Svelte + TypeScript (Vite)
- API Gateway: ASP.NET Core (YARP)
- Backend microservices: authentication, gyms, memberships, notifications, workout logging
- Messaging: RabbitMQ for async events
- Deployments: Docker Compose (local) and Kubernetes (Minikube/GKE)
- Security/quality tooling: OWASP ZAP runners, SonarQube setup

## High-level architecture

Browser → Frontend → API Gateway → Microservices → Databases

## Run locally (Docker Compose)

```bash
cd deployment/docker
docker-compose pull
docker-compose up -d
```

Typical endpoints (depends on compose config):

- Frontend: `http://localhost:3000`
- API Gateway: `http://localhost:5000`

## Local development (dev mode)

Run services locally and keep dependencies in containers.

```bash
# Frontend
cd GymHiveFrontend
npm install
npm run dev
```

Backends (examples):

```bash
cd GymHiveBackend/ApiGateway
dotnet run
```

## Kubernetes

See [deployment/kubernetes/README.md](deployment/kubernetes/README.md).

## Security scanning (OWASP ZAP)

See [security/zap/README.md](security/zap/README.md).

## SonarQube analysis

See [SONARQUBE-README.md](SONARQUBE-README.md).

## Load testing

See [load-tests/README.md](load-tests/README.md).

## GDPR / privacy notes

See [GDPR-README.md](GDPR-README.md).
