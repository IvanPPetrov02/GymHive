# GymHive Kubernetes Deployment

This directory contains Kubernetes configurations for deploying GymHive microservices.

## Prerequisites

- **Minikube** or **Google Kubernetes Engine (GKE)**
- **kubectl** CLI tool
- Docker images published to Docker Hub: `ivanppetrov/gymhive-*-service:latest`

## Architecture

### Services (7 total)
- **auth-service** - Authentication and authorization (Port 8080)
- **gym-service** - Gym management (Port 8080)
- **membership-service** - Membership management with MongoDB (Port 8080)
- **notifications-service** - Notification system (Port 8080)
- **workout-service** - Workout logging (Port 8080)
- **api-gateway** - Reverse proxy with YARP (Port 80/30500)
- **frontend** - Svelte web application (Port 80/30080)

### Infrastructure
- **RabbitMQ** - Message broker for async communication
- **MongoDB** - NoSQL database for memberships
- **MySQL x4** - Databases for auth, gym, notifications, workout services
- **Prometheus** - Metrics collection
- **Grafana** - Monitoring dashboards

## Quick Start - Local (Minikube)

### 1. Start Minikube
```powershell
minikube start --driver=docker --cpus=4 --memory=8192
```

### 2. Deploy GymHive
```powershell
cd deployment/kubernetes
.\deploy-k8s.ps1
```

This script will:
1. Create `gymhive` namespace
2. Deploy monitoring stack (Prometheus & Grafana)
3. Deploy databases (MySQL x4, MongoDB, RabbitMQ)
4. Deploy application services (7 microservices)
5. Set up port-forwarding automatically

### 3. Access the Application

The deployment script automatically sets up port-forwarding:

- **Frontend**: http://localhost:3000
- **API Gateway**: http://localhost:5000
- **Grafana**: http://localhost:3001 (admin/admin)
- **Prometheus**: http://localhost:9090

## Manual Deployment Steps

If you prefer manual deployment:

```powershell
# Create namespace
kubectl apply -f namespace.yaml

# Deploy monitoring
kubectl apply -f monitoring-k8s/

# Deploy databases and wait
kubectl apply -f databases/
kubectl wait --for=condition=ready pod -l app=rabbitmq -n gymhive --timeout=300s

# Deploy services
kubectl apply -f services/

# Check status
kubectl get pods -n gymhive
kubectl get svc -n gymhive
kubectl get hpa -n gymhive
```

## Port Forwarding (Manual)

```powershell
# Frontend
kubectl port-forward service/frontend 3000:80 -n gymhive

# API Gateway
kubectl port-forward service/api-gateway 5000:80 -n gymhive

# Grafana
kubectl port-forward service/grafana 3001:3000 -n gymhive

# Prometheus
kubectl port-forward service/prometheus 9090:9090 -n gymhive

# RabbitMQ Management
kubectl port-forward service/rabbitmq 15672:15672 -n gymhive
```

## Google Kubernetes Engine (GKE) Deployment

### 1. Create GKE Cluster
```bash
gcloud container clusters create gymhive-cluster \
  --num-nodes=3 \
  --machine-type=e2-standard-2 \
  --zone=us-central1-a \
  --enable-autoscaling \
  --min-nodes=3 \
  --max-nodes=10
```

### 2. Get Credentials
```bash
gcloud container clusters get-credentials gymhive-cluster --zone=us-central1-a
```

### 3. Deploy Application
```powershell
kubectl apply -f namespace.yaml
kubectl apply -f monitoring-k8s/
kubectl apply -f databases/
kubectl apply -f services/
```

### 4. Expose Frontend (LoadBalancer)
```bash
kubectl get svc frontend -n gymhive
# Wait for EXTERNAL-IP to be assigned
```

The frontend will be accessible at the external IP provided by GKE.

## Useful Commands

### View Logs
```powershell
# Single service
kubectl logs -f deployment/auth-service -n gymhive

# All pods for a service
kubectl logs -l app=auth-service -n gymhive --tail=100

# Previous crashed container
kubectl logs deployment/auth-service -n gymhive --previous
```

### Scale Services
```powershell
# Manual scaling
kubectl scale deployment auth-service --replicas=5 -n gymhive

# Check HPA status
kubectl get hpa -n gymhive

# Describe HPA
kubectl describe hpa auth-service-hpa -n gymhive
```

### Debug Pods
```powershell
# Get pod details
kubectl describe pod <pod-name> -n gymhive

# Execute command in pod
kubectl exec -it <pod-name> -n gymhive -- /bin/sh

# Get pod YAML
kubectl get pod <pod-name> -n gymhive -o yaml
```

### Watch Resources
```powershell
# Watch pods
kubectl get pods -n gymhive --watch

# Watch HPA
kubectl get hpa -n gymhive --watch

# Watch all resources
kubectl get all -n gymhive
```

### Cleanup
```powershell
# Delete everything (including data!)
kubectl delete namespace gymhive

# Or delete individual resources
kubectl delete -f services/
kubectl delete -f databases/
kubectl delete -f monitoring-k8s/
```

## Configuration Details

### Environment Variables

All services are configured via environment variables in their respective YAML files:

**RabbitMQ Configuration** (auth, gym, membership, notifications, workout):
```yaml
- name: RabbitMQ__HostName
  value: "rabbitmq"
- name: RabbitMQ__Port
  value: "5672"
- name: RabbitMQ__UserName
  value: "gymhive"
- name: RabbitMQ__Password
  value: "GymHive123!"
- name: RabbitMQ__VirtualHost
  value: "/"
```

**MongoDB Configuration** (membership-service):
```yaml
- name: MongoDB__ConnectionString
  value: "mongodb://root:RootPassword123!@mongodb:27017"
- name: MongoDB__DatabaseName
  value: "GymHiveMembershipsV2"
```

**MySQL Configuration** (auth, gym, notifications, workout):
```yaml
- name: ConnectionStrings__DefaultConnection
  value: "Server=<db-name>;Port=3306;Database=<db-name>;User=gymhive_user;Password=GymHive123!;"
```

### Resource Limits

All services have resource requests and limits:
```yaml
resources:
  requests:
    memory: "256Mi"
    cpu: "100m"
  limits:
    memory: "512Mi"
    cpu: "500m"
```

### Auto-Scaling (HPA)

All microservices have Horizontal Pod Autoscalers:
- **Min Replicas**: 1
- **Max Replicas**: 10
- **CPU Target**: 50%
- **Memory Target**: 70%

### Persistent Storage

All databases use PersistentVolumeClaims:
- **auth-db**: 1Gi
- **gym-db**: 1Gi
- **notifications-db**: 1Gi
- **workout-db**: 1Gi
- **mongodb**: 2Gi
- **rabbitmq**: 1Gi

## Monitoring

### Prometheus Metrics

Access Prometheus at http://localhost:9090 (when port-forwarded)

Example queries:
```promql
# CPU usage by pod
container_cpu_usage_seconds_total{namespace="gymhive"}

# Memory usage by pod
container_memory_usage_bytes{namespace="gymhive"}

# Request rate
rate(http_requests_total{namespace="gymhive"}[5m])
```

### Grafana Dashboards

Access Grafana at http://localhost:3001 (admin/admin)

Pre-configured dashboards for:
- Kubernetes cluster overview
- Pod resource usage
- Service metrics
- Database performance

## Troubleshooting

### Pods in CrashLoopBackOff
```powershell
# Check logs
kubectl logs deployment/<service-name> -n gymhive

# Check events
kubectl describe pod <pod-name> -n gymhive
```

### Database Connection Issues
```powershell
# Check if database is ready
kubectl get pods -n gymhive | grep db

# Test connection from service pod
kubectl exec -it <service-pod> -n gymhive -- /bin/sh
# Then try: curl http://<db-name>:3306
```

### Image Pull Errors
- Ensure images are public on Docker Hub: `ivanppetrov/gymhive-*-service:latest`
- Check image pull policy in deployment YAML

### Service Not Accessible
```powershell
# Check service endpoints
kubectl get endpoints -n gymhive

# Check if service selector matches pod labels
kubectl describe svc <service-name> -n gymhive
kubectl get pods -n gymhive --show-labels
```

## Production Checklist

Before deploying to GKE production:

- [ ] Change default passwords in all database configurations
- [ ] Set up proper secrets management (Kubernetes Secrets or Google Secret Manager)
- [ ] Configure SSL/TLS for external endpoints
- [ ] Set up proper ingress with domain names
- [ ] Configure backup strategies for databases
- [ ] Set up log aggregation (e.g., Google Cloud Logging)
- [ ] Configure proper resource limits based on load testing
- [ ] Set up alerting (e.g., Google Cloud Monitoring)
- [ ] Review and tighten RBAC permissions
- [ ] Enable Pod Security Policies
- [ ] Configure network policies for service isolation

## Support

For issues or questions:
1. Check logs: `kubectl logs deployment/<service-name> -n gymhive`
2. Check pod status: `kubectl get pods -n gymhive`
3. Review events: `kubectl get events -n gymhive --sort-by='.lastTimestamp'`
