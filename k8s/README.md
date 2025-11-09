# Kubernetes Deployment for GymHive

This directory contains all Kubernetes manifests for deploying GymHive to a Kubernetes cluster.

## Prerequisites

- Kubernetes cluster (Minikube, Docker Desktop, or cloud provider)
- kubectl installed and configured
- Docker images pushed to Docker Hub

## Quick Start

### 1. Create Namespace
```bash
kubectl apply -f namespace.yaml
```

### 2. Deploy Databases
```bash
kubectl apply -f databases/
```

### 3. Deploy Services
```bash
kubectl apply -f services/
```

### 4. Verify Deployment
```bash
kubectl get pods -n gymhive
kubectl get services -n gymhive
```

## Deployment Order

1. `namespace.yaml` - Create gymhive namespace
2. `databases/` - MySQL databases with persistent storage
3. `services/auth-service.yaml` - Authentication service
4. `services/gym-service.yaml` - Gym service
5. `services/membership-service.yaml` - Membership service
6. `services/api-gateway.yaml` - API Gateway
7. `services/frontend.yaml` - Frontend application
8. `monitoring/` - Prometheus and Grafana (optional)

## Configuration

Update Docker image names in service YAML files:
- Replace `yourusername` with your Docker Hub username
- Update image tags as needed (`:latest`, `:dev`, etc.)

## Access Application

After deployment, access the application:

**Using LoadBalancer (Cloud):**
```bash
kubectl get service frontend -n gymhive
# Use EXTERNAL-IP
```

**Using NodePort (Local):**
```bash
kubectl get service frontend -n gymhive
# Use NodePort (e.g., 30080)
# Access: http://localhost:30080
```

**Using Port Forward (Development):**
```bash
kubectl port-forward service/frontend 3000:80 -n gymhive
# Access: http://localhost:3000
```

## Scaling

Scale services as needed:
```bash
kubectl scale deployment auth-service --replicas=3 -n gymhive
kubectl scale deployment gym-service --replicas=3 -n gymhive
```

## Monitoring

View logs:
```bash
kubectl logs -f deployment/auth-service -n gymhive
```

Check pod status:
```bash
kubectl describe pod <pod-name> -n gymhive
```

## Cleanup

Remove all resources:
```bash
kubectl delete namespace gymhive
```
