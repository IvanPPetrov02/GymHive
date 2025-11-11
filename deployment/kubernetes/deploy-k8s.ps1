# GymHive Kubernetes Quick Deploy Script
# This script deploys the entire GymHive application to Kubernetes

Write-Host "GymHive Kubernetes Deployment" -ForegroundColor Green
Write-Host "=================================" -ForegroundColor Green
Write-Host ""

# Check if kubectl is available
Write-Host "Checking prerequisites..." -ForegroundColor Yellow
try {
    kubectl version --client --short 2>&1 | Out-Null
    Write-Host "kubectl is installed" -ForegroundColor Green
} catch {
    Write-Host "kubectl is not installed. Please install kubectl first." -ForegroundColor Red
    exit 1
}

# Check if Kubernetes is running
Write-Host "Checking Kubernetes cluster..." -ForegroundColor Yellow
try {
    kubectl cluster-info 2>&1 | Out-Null
    Write-Host "Kubernetes cluster is running" -ForegroundColor Green
} catch {
    Write-Host "Kubernetes cluster is not running. Please start Docker Desktop Kubernetes." -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "Starting deployment..." -ForegroundColor Cyan
Write-Host ""

Write-Host "Step 1: Creating namespace..." -ForegroundColor Yellow
kubectl apply -f namespace.yaml
Write-Host "Namespace created" -ForegroundColor Green

Write-Host ""
Write-Host "Step 2: Deploying monitoring stack..." -ForegroundColor Yellow
kubectl apply -f monitoring-k8s/
Write-Host "Monitoring stack deployed (Prometheus & Grafana)" -ForegroundColor Green

Write-Host ""
Write-Host "Step 3: Deploying databases..." -ForegroundColor Yellow
kubectl apply -f databases/
Write-Host "Waiting for databases to be ready (this may take 2-3 minutes)..." -ForegroundColor Yellow

# Wait for databases
kubectl wait --for=condition=ready pod -l app=auth-db -n gymhive --timeout=300s 2>&1 | Out-Null
kubectl wait --for=condition=ready pod -l app=gym-db -n gymhive --timeout=300s 2>&1 | Out-Null
kubectl wait --for=condition=ready pod -l app=membership-db -n gymhive --timeout=300s 2>&1 | Out-Null

Write-Host ""
Write-Host "Step 3: Deploying databases..." -ForegroundColor Yellow
kubectl apply -f databases/
Write-Host "Waiting for databases to be ready (this may take 2-3 minutes)..." -ForegroundColor Yellow

# Wait for databases
kubectl wait --for=condition=ready pod -l app=auth-db -n gymhive --timeout=300s 2>&1 | Out-Null
kubectl wait --for=condition=ready pod -l app=gym-db -n gymhive --timeout=300s 2>&1 | Out-Null
kubectl wait --for=condition=ready pod -l app=membership-db -n gymhive --timeout=300s 2>&1 | Out-Null

Write-Host "Databases are ready" -ForegroundColor Green

Write-Host ""
Write-Host "Step 4: Deploying application services..." -ForegroundColor Yellow
kubectl apply -f services/
Write-Host "Waiting for services to be ready (this may take 2-3 minutes)..." -ForegroundColor Yellow

# Wait for services
kubectl wait --for=condition=ready pod -l app=auth-service -n gymhive --timeout=300s 2>&1 | Out-Null
kubectl wait --for=condition=ready pod -l app=gym-service -n gymhive --timeout=300s 2>&1 | Out-Null
kubectl wait --for=condition=ready pod -l app=membership-service -n gymhive --timeout=300s 2>&1 | Out-Null
kubectl wait --for=condition=ready pod -l app=api-gateway -n gymhive --timeout=300s 2>&1 | Out-Null
kubectl wait --for=condition=ready pod -l app=frontend -n gymhive --timeout=300s 2>&1 | Out-Null

Write-Host "All services are ready" -ForegroundColor Green

Write-Host ""
Write-Host "=================================" -ForegroundColor Green
Write-Host "Deployment Complete!" -ForegroundColor Green
Write-Host "=================================" -ForegroundColor Green
Write-Host ""

# Show deployment status
Write-Host "Deployment Status:" -ForegroundColor Cyan
Write-Host ""
kubectl get pods -n gymhive

Write-Host ""
Write-Host "Services:" -ForegroundColor Cyan
Write-Host ""
kubectl get services -n gymhive

Write-Host ""
Write-Host "Auto-scalers (HPA):" -ForegroundColor Cyan
Write-Host ""
kubectl get hpa -n gymhive

Write-Host ""
Write-Host "Access Your Application:" -ForegroundColor Green
Write-Host ""
Write-Host "Option 1: Port Forward (Recommended)" -ForegroundColor Yellow
Write-Host "  kubectl port-forward service/frontend 3000:80 -n gymhive" -ForegroundColor White
Write-Host "  Then visit: http://localhost:3000" -ForegroundColor Cyan
Write-Host ""
Write-Host "Option 2: NodePort" -ForegroundColor Yellow
Write-Host "  Frontend: http://localhost:30080" -ForegroundColor Cyan
Write-Host "  Grafana:  http://localhost:30030 (admin/admin)" -ForegroundColor Cyan
Write-Host "  Prometheus: http://localhost:30090" -ForegroundColor Cyan
Write-Host ""

Write-Host "Useful Commands:" -ForegroundColor Green
Write-Host ""
Write-Host "View logs:        kubectl logs -f deployment/auth-service -n gymhive" -ForegroundColor White
Write-Host "Scale service:    kubectl scale deployment auth-service --replicas=5 -n gymhive" -ForegroundColor White
Write-Host "Watch pods:       kubectl get pods -n gymhive --watch" -ForegroundColor White
Write-Host "Delete all:       kubectl delete namespace gymhive" -ForegroundColor White
Write-Host ""
