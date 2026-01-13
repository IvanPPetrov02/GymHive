# GymHive — Kubernetes deployment

This folder contains Kubernetes manifests and helper scripts for deploying GymHive to a Kubernetes cluster (Minikube or GKE).

## What was done

- Added Kubernetes manifests for services and supporting infrastructure.
- Added a PowerShell deploy script for repeatable installs.

## Prerequisites

- `kubectl`
- A Kubernetes cluster (Minikube or GKE)
- Container images available in a registry (Docker Hub or your own)

## Deploy (Minikube)

```powershell
minikube start --driver=docker --cpus=4 --memory=8192

cd deployment/kubernetes
./deploy-k8s.ps1
```

## Deploy (GKE)

Create cluster (Autopilot example):

```bash
gcloud container clusters create-auto gymhive-autopilot --region=europe-west1
gcloud container clusters get-credentials gymhive-autopilot --region=europe-west1
```

Apply manifests:

```powershell
cd deployment/kubernetes
kubectl apply -f namespace.yaml
kubectl apply -f databases/
kubectl apply -f services/
```

## Access services

Depending on the deployment method, use Ingress/LoadBalancer or port-forward locally.

```powershell
kubectl port-forward service/frontend 3000:80 -n gymhive
kubectl port-forward service/api-gateway 5000:80 -n gymhive
```

## Troubleshooting

```powershell
kubectl get pods -n gymhive
kubectl get svc -n gymhive
kubectl describe pod <pod-name> -n gymhive
kubectl logs -f deployment/<deployment-name> -n gymhive
```
