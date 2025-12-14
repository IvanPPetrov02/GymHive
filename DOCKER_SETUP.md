# GymHive Docker Setup Guide

This guide explains how to build and run the entire GymHive application stack using Docker.

## Prerequisites

- Docker Desktop installed and running
- At least 8GB of RAM allocated to Docker
- PowerShell (for Windows)

## Quick Start

### 1. Using the Setup Script (Recommended)

The easiest way to get started is to use the automated setup script:

```powershell
.\docker-setup.ps1
```

This script will:
- Check if Docker is running
- Load environment variables from `.env`
- Build all Docker images
- Start all services
- Display service URLs

### 2. Manual Setup

If you prefer manual control:

#### Build all images:
```powershell
docker-compose build
```

#### Start all services:
```powershell
docker-compose up -d
```

#### View logs:
```powershell
docker-compose logs -f
```

#### Stop all services:
```powershell
docker-compose down
```

## Architecture

The application consists of the following services:

### Infrastructure Services:
- **RabbitMQ** (Port 5672, Management UI: 15672) - Message broker
- **MySQL Databases** - For Auth, Gym, Notifications, Workout services
- **MongoDB** (Port 27017) - For Membership service

### Microservices:
- **Authentication Service** (Port 8080) - User authentication and JWT tokens
- **Gym Service** (Port 8081) - Gym management
- **Membership Service** (Port 8082) - Membership management
- **Notifications Service** (Port 8083) - User notifications
- **Workout Logging Service** (Port 8084) - Workout tracking
- **API Gateway** (Port 5000) - Unified API entry point
- **Frontend** (Port 3000) - Web application

## Environment Variables

All configuration is managed through the `.env` file in the root directory. Key variables include:

```env
# JWT Configuration
JWT_SECRET=YourSecretKey
JWT_ISSUER=GymHive
JWT_AUDIENCE=GymHive

# Database Credentials
AUTH_DB_NAME=GymHive
AUTH_DB_PASSWORD=yourpassword
# ... (similar for other databases)

# RabbitMQ
RABBITMQ_USER=gymhive
RABBITMQ_PASSWORD=GymHive123!

# MongoDB
MONGODB_CONNECTION_STRING=mongodb://localhost:27017
```

## Service Health Checks

All services include health checks. You can verify service status with:

```powershell
docker-compose ps
```

Healthy services will show `(healthy)` in the status column.

## Accessing Services

Once all services are running:

- **Frontend**: http://localhost:3000
- **API Gateway**: http://localhost:5000
- **RabbitMQ Management**: http://localhost:15672 (user: gymhive, password from .env)
- **Individual Services**: Ports 8080-8084

## Troubleshooting

### Services won't start
1. Check if all required ports are available
2. Ensure Docker has enough resources (8GB RAM minimum)
3. Check logs: `docker-compose logs [service-name]`

### Database connection errors
1. Wait for databases to be fully healthy: `docker-compose ps`
2. Check database credentials in `.env`
3. Verify database containers are running

### Build failures
1. Ensure all dependencies are available
2. Check Dockerfile syntax
3. Try cleaning Docker cache: `docker system prune -a`

### Individual Service Debugging

To debug a specific service:

```powershell
# View logs for a service
docker-compose logs -f auth-service

# Restart a service
docker-compose restart auth-service

# Rebuild and restart
docker-compose up -d --build auth-service
```

## Development Workflow

### Rebuilding After Code Changes

```powershell
# Rebuild specific service
docker-compose build auth-service
docker-compose up -d auth-service

# Rebuild all services
docker-compose build
docker-compose up -d
```

### Accessing Container Shell

```powershell
docker exec -it gymhive-auth-service /bin/bash
```

### Viewing Real-time Logs

```powershell
# All services
docker-compose logs -f

# Specific service
docker-compose logs -f auth-service
```

## Database Management

### MySQL Databases

Connect to MySQL databases:
```powershell
docker exec -it gymhive-auth-db mysql -uroot -p
```

### MongoDB

Connect to MongoDB:
```powershell
docker exec -it gymhive-mongodb mongosh
```

## Clean Up

### Stop services (keep volumes):
```powershell
docker-compose down
```

### Stop services and remove volumes (fresh start):
```powershell
docker-compose down -v
```

### Remove all GymHive images:
```powershell
docker images | Select-String "gymhive" | ForEach-Object { docker rmi ($_ -split '\s+')[0] }
```

## Production Deployment

For production deployment:

1. Update `ASPNETCORE_ENVIRONMENT=Production` in `.env`
2. Use strong passwords for all services
3. Enable HTTPS/TLS
4. Use Docker Swarm or Kubernetes for orchestration
5. Set up monitoring and logging
6. Configure backups for databases

## Network Architecture

All services communicate through the `gymhive-network` bridge network. Services can reach each other using their container names (e.g., `http://auth-service:8080`).

## Volume Management

Persistent data is stored in Docker volumes:
- `auth-db-data` - Authentication database
- `gym-db-data` - Gym database
- `mongodb-data` - Membership data
- `notifications-db-data` - Notifications database
- `workout-db-data` - Workout logs
- `rabbitmq-data` - Message broker data

To backup volumes:
```powershell
docker run --rm -v gymhive_auth-db-data:/data -v ${PWD}:/backup alpine tar czf /backup/auth-db-backup.tar.gz /data
```

## Support

For issues or questions:
1. Check the troubleshooting section
2. Review service logs
3. Consult the main project README
