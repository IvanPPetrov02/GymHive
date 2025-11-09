# GymHive Kubernetes Deployment Guide

## 📋 Prerequisites

1. **Kubernetes Cluster**
   - **Local Development:** Docker Desktop with Kubernetes enabled OR Minikube
   - **Cloud:** Azure AKS, AWS EKS, or Google GKE

2. **Tools Required:**
   - `kubectl` - Kubernetes CLI
   - Docker images pushed to Docker Hub

3. **Update Image Names:**
   Before deploying, update all YAML files in `k8s/services/` with your Docker Hub username:
   ```yaml
   image: yourusername/gymhive-auth-service:latest
   ```
   Replace `yourusername` with your actual Docker Hub username.

## 🚀 Quick Deployment

### Step 1: Enable Kubernetes (Docker Desktop)

**Windows:**
1. Open Docker Desktop
2. Click Settings → Kubernetes
3. Check "Enable Kubernetes"
4. Click "Apply & Restart"
5. Wait for Kubernetes to start (green icon)

**Verify:**
```powershell
kubectl version --client
kubectl cluster-info
```

### Step 2: Create Namespace

```powershell
kubectl apply -f k8s/namespace.yaml
```

**Verify:**
```powershell
kubectl get namespaces
```

### Step 3: Deploy Databases

```powershell
kubectl apply -f k8s/databases/
```

**Wait for databases to be ready:**
```powershell
kubectl wait --for=condition=ready pod -l app=auth-db -n gymhive --timeout=300s
kubectl wait --for=condition=ready pod -l app=gym-db -n gymhive --timeout=300s
kubectl wait --for=condition=ready pod -l app=membership-db -n gymhive --timeout=300s
```

### Step 4: Deploy Application Services

```powershell
kubectl apply -f k8s/services/
```

**Wait for services to be ready:**
```powershell
kubectl wait --for=condition=ready pod -l app=auth-service -n gymhive --timeout=300s
kubectl wait --for=condition=ready pod -l app=gym-service -n gymhive --timeout=300s
kubectl wait --for=condition=ready pod -l app=membership-service -n gymhive --timeout=300s
kubectl wait --for=condition=ready pod -l app=api-gateway -n gymhive --timeout=300s
kubectl wait --for=condition=ready pod -l app=frontend -n gymhive --timeout=300s
```

### Step 5: Verify Deployment

**Check all pods:**
```powershell
kubectl get pods -n gymhive
```

Expected output (all Running):
```
NAME                                  READY   STATUS    RESTARTS   AGE
auth-db-xxxxx                         1/1     Running   0          5m
gym-db-xxxxx                          1/1     Running   0          5m
membership-db-xxxxx                   1/1     Running   0          5m
auth-service-xxxxx                    1/1     Running   0          3m
auth-service-yyyyy                    1/1     Running   0          3m
gym-service-xxxxx                     1/1     Running   0          3m
gym-service-yyyyy                     1/1     Running   0          3m
membership-service-xxxxx              1/1     Running   0          3m
membership-service-yyyyy              1/1     Running   0          3m
api-gateway-xxxxx                     1/1     Running   0          3m
api-gateway-yyyyy                     1/1     Running   0          3m
frontend-xxxxx                        1/1     Running   0          3m
frontend-yyyyy                        1/1     Running   0          3m
```

**Check services:**
```powershell
kubectl get services -n gymhive
```

**Check HPA (Horizontal Pod Autoscalers):**
```powershell
kubectl get hpa -n gymhive
```

## 🌐 Access the Application

### Option 1: LoadBalancer (Cloud or Docker Desktop)

**Get the external IP:**
```powershell
kubectl get service frontend -n gymhive
```

Look for `EXTERNAL-IP`:
- **Docker Desktop:** Will be `localhost`
- **Cloud:** Will show an IP address

Access at: `http://<EXTERNAL-IP>`

### Option 2: NodePort (Local Development)

```powershell
kubectl get service frontend -n gymhive
```

Look for `PORT(S)`: `80:30080/TCP`

Access at: `http://localhost:30080`

### Option 3: Port Forward (Development/Testing)

```powershell
kubectl port-forward service/frontend 3000:80 -n gymhive
```

Access at: `http://localhost:3000`

**API Gateway:**
```powershell
kubectl port-forward service/api-gateway 5000:80 -n gymhive
```

Access at: `http://localhost:5000`

## 📊 Monitoring & Management

### View Logs

**Specific pod:**
```powershell
kubectl logs -f <pod-name> -n gymhive
```

**All pods of a deployment:**
```powershell
kubectl logs -f deployment/auth-service -n gymhive
```

**Previous logs (if pod crashed):**
```powershell
kubectl logs <pod-name> --previous -n gymhive
```

### Describe Resources

**Pod details:**
```powershell
kubectl describe pod <pod-name> -n gymhive
```

**Deployment details:**
```powershell
kubectl describe deployment auth-service -n gymhive
```

**Service details:**
```powershell
kubectl describe service api-gateway -n gymhive
```

### Execute Commands in Pods

**Get a shell:**
```powershell
kubectl exec -it <pod-name> -n gymhive -- /bin/bash
```

**Run a command:**
```powershell
kubectl exec <pod-name> -n gymhive -- env
```

### Check Resource Usage

```powershell
kubectl top pods -n gymhive
kubectl top nodes
```

## ⚖️ Scaling

### Manual Scaling

**Scale a specific deployment:**
```powershell
kubectl scale deployment auth-service --replicas=5 -n gymhive
kubectl scale deployment gym-service --replicas=3 -n gymhive
```

**Scale all services:**
```powershell
kubectl scale deployment --all --replicas=3 -n gymhive
```

### Automatic Scaling (HPA)

The Horizontal Pod Autoscalers are already configured:
- **Min replicas:** 2
- **Max replicas:** 10
- **Target CPU:** 70%
- **Target Memory:** 80%

**View HPA status:**
```powershell
kubectl get hpa -n gymhive
kubectl describe hpa auth-service-hpa -n gymhive
```

**Modify HPA:**
```powershell
kubectl edit hpa auth-service-hpa -n gymhive
```

## 🔄 Rolling Updates

### Update Image

**Method 1: Edit deployment**
```powershell
kubectl set image deployment/auth-service auth-service=yourusername/gymhive-auth-service:v2 -n gymhive
```

**Method 2: Apply updated YAML**
```powershell
kubectl apply -f k8s/services/auth-service.yaml
```

**Watch rollout:**
```powershell
kubectl rollout status deployment/auth-service -n gymhive
```

### Rollback

**Undo last deployment:**
```powershell
kubectl rollout undo deployment/auth-service -n gymhive
```

**Rollback to specific revision:**
```powershell
kubectl rollout history deployment/auth-service -n gymhive
kubectl rollout undo deployment/auth-service --to-revision=2 -n gymhive
```

## 🧪 Testing Load & Autoscaling

### Generate Load

**Using k6 (from your load tests):**
```powershell
# Get the service URL
kubectl get service api-gateway -n gymhive

# Run load test (update URL in test file first)
k6 run load-tests/full-system-test.js
```

**Watch pods scale automatically:**
```powershell
kubectl get hpa -n gymhive --watch
```

You should see replicas increase as CPU/memory usage goes up!

### Stress Test a Specific Service

```powershell
# Port forward to API Gateway
kubectl port-forward service/api-gateway 5000:80 -n gymhive

# Run load test
k6 run load-tests/auth-load-test.js
```

**Monitor autoscaling:**
```powershell
kubectl get pods -n gymhive --watch
```

## 🔧 Troubleshooting

### Pods Not Starting

**Check pod status:**
```powershell
kubectl get pods -n gymhive
kubectl describe pod <pod-name> -n gymhive
```

**Common issues:**
- **ImagePullBackOff:** Docker image not found or private
  - Solution: Check image name, ensure it's pushed to Docker Hub
- **CrashLoopBackOff:** Application crashing
  - Solution: Check logs: `kubectl logs <pod-name> -n gymhive`
- **Pending:** Not enough resources
  - Solution: Check node resources: `kubectl describe node`

### Database Connection Issues

**Check if databases are running:**
```powershell
kubectl get pods -l app=auth-db -n gymhive
kubectl get pods -l app=gym-db -n gymhive
```

**Test database connection from a pod:**
```powershell
kubectl exec -it <service-pod> -n gymhive -- /bin/bash
# Inside pod:
ping auth-db
```

### Service Not Accessible

**Check service endpoints:**
```powershell
kubectl get endpoints -n gymhive
```

**Check if pods are ready:**
```powershell
kubectl get pods -n gymhive
```

Look for `READY 1/1` - both containers should be ready.

## 🧹 Cleanup

### Delete Everything

**Delete namespace (removes all resources):**
```powershell
kubectl delete namespace gymhive
```

**Delete specific resources:**
```powershell
kubectl delete -f k8s/services/
kubectl delete -f k8s/databases/
kubectl delete -f k8s/namespace.yaml
```

### Delete and Redeploy

```powershell
# Quick cleanup and redeploy
kubectl delete namespace gymhive
kubectl apply -f k8s/namespace.yaml
kubectl apply -f k8s/databases/
kubectl apply -f k8s/services/
```

## 📈 Production Best Practices

### 1. Use Secrets for Sensitive Data

Create secrets for database passwords:
```powershell
kubectl create secret generic db-credentials `
  --from-literal=root-password=RootPassword123! `
  --from-literal=user-password=GymHive123! `
  -n gymhive
```

Reference in deployments:
```yaml
env:
- name: MYSQL_ROOT_PASSWORD
  valueFrom:
    secretKeyRef:
      name: db-credentials
      key: root-password
```

### 2. Resource Limits

Already configured in all deployments:
```yaml
resources:
  requests:
    memory: "256Mi"
    cpu: "100m"
  limits:
    memory: "512Mi"
    cpu: "500m"
```

### 3. Health Checks

Already configured:
- **Liveness Probe:** Restarts pod if unhealthy
- **Readiness Probe:** Removes from service if not ready

### 4. Persistent Storage

Already configured with PersistentVolumeClaims for databases.

### 5. Namespaces

Using `gymhive` namespace for isolation.

## 🎓 Kubernetes Features Demonstrated

✅ **Deployments** - Declarative updates for pods
✅ **Services** - Load balancing and service discovery
✅ **Persistent Volumes** - Database data persistence
✅ **Horizontal Pod Autoscaling** - Automatic scaling based on CPU/memory
✅ **ConfigMaps & Secrets** - Configuration management
✅ **Health Checks** - Liveness and readiness probes
✅ **Resource Limits** - CPU and memory management
✅ **Namespaces** - Logical separation
✅ **Rolling Updates** - Zero-downtime deployments
✅ **Load Balancing** - Automatic load distribution

---

## 📚 Additional Resources

- Kubernetes Documentation: https://kubernetes.io/docs/
- kubectl Cheat Sheet: https://kubernetes.io/docs/reference/kubectl/cheatsheet/
- Docker Desktop Kubernetes: https://docs.docker.com/desktop/kubernetes/

**Your GymHive application is now running on Kubernetes!** 🎉
